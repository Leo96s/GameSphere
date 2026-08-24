#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.ServicesResponses;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class UserSessionTests : UserAccountTestBase, IClassFixture<PostgreSqlFixture>
{
    public UserSessionTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Registration_and_login_create_a_valid_authenticated_session()
    {
        var email = $"registration-{Guid.NewGuid():N}@example.test";
        const string password = "Registration-password-123";

        var registrationResponse = await Client.PostAsJsonAsync("/api/User", new
        {
            firstName = "New",
            lastName = "Player",
            email,
            password,
            gender = (int)Gender.OUTRO,
            isActive = true,
        }, TestContext.Current.CancellationToken);

        var registrationBody = await registrationResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(registrationResponse.StatusCode == HttpStatusCode.Created, registrationBody);

        var loginResponse = await Client.PostAsJsonAsync("/api/User/login", new
        {
            email,
            password,
        }, TestContext.Current.CancellationToken);
        var loginBody = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginDocument = JsonDocument.Parse(loginBody);
        Assert.DoesNotContain("\"token\"", loginBody, StringComparison.OrdinalIgnoreCase);
        Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains("gamesphere_access_token=", StringComparison.Ordinal));
        Assert.DoesNotContain("hashedPassword", loginBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_session_cookie_has_browser_security_attributes()
    {
        var (owner, _) = await SeedUsersAsync();

        var response = await Client.PostAsJsonAsync("/api/User/login", new
        {
            email = owner.Email,
            password = "Test-password-123",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var accessCookie = Assert.Single(cookies, cookie => cookie.StartsWith("gamesphere_access_token=", StringComparison.Ordinal));
        Assert.Contains("HttpOnly", accessCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Lax", accessCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/", accessCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_clears_the_session_cookie()
    {
        var response = await Client.PostAsync("/api/User/logout", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var clearedCookie = Assert.Single(cookies, cookie => cookie.StartsWith("gamesphere_access_token=", StringComparison.Ordinal));
        Assert.Contains("expires=Thu, 01 Jan 1970", clearedCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpOnly", clearedCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Social_login_requires_a_verified_firebase_token()
    {
        var user = CreateUser($"social-{Guid.NewGuid():N}@example.test", "Social");
        user.UID = "firebase-user-id";
        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await Client.PostAsJsonAsync(
            "/api/User/social-login",
            new { idToken = string.Empty },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Social_login_rejects_conflicting_verified_identity()
    {
        var email = $"social-conflict-{Guid.NewGuid():N}@example.test";
        var existingUser = CreateUser(email, "Existing");
        existingUser.UID = "firebase-existing-user";

        await using (var context = CreateContext())
        {
            context.Users.Add(existingUser);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        FirebaseTokenVerifier.ExpectedToken = "verified-conflict-token";
        FirebaseTokenVerifier.User = new FirebaseUserInfo("firebase-different-user", email, "Conflicting Player");

        var response = await Client.PostAsJsonAsync(
            "/api/User/social-login",
            new { idToken = FirebaseTokenVerifier.ExpectedToken },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("The social login could not be completed.", body, StringComparison.Ordinal);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));

        await using var verificationContext = CreateContext();
        var persistedUser = await verificationContext.Users.SingleAsync(candidate => candidate.Email == email, TestContext.Current.CancellationToken);
        Assert.Equal("firebase-existing-user", persistedUser.UID);
        Assert.Equal(1, await verificationContext.Users.CountAsync(candidate => candidate.Email == email, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Social_login_with_a_verified_token_creates_an_authenticated_session()
    {
        var email = $"social-{Guid.NewGuid():N}@example.test";
        FirebaseTokenVerifier.ExpectedToken = "verified-firebase-token";
        FirebaseTokenVerifier.User = new FirebaseUserInfo("firebase-new-user", email, "Social Player");

        var response = await Client.PostAsJsonAsync(
            "/api/User/social-login",
            new { idToken = FirebaseTokenVerifier.ExpectedToken },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("firebase-new-user", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.StartsWith("gamesphere_access_token=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_deactivated_user_cannot_use_an_existing_token()
    {
        var (owner, _) = await SeedUsersAsync();
        await using (var context = CreateContext())
        {
            var persistedOwner = await context.Users.SingleAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken);
            persistedOwner.isActive = false;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/quizzes", owner);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Password_change_revokes_a_token_issued_before_the_change()
    {
        var (owner, _) = await SeedUsersAsync();
        using var updateRequest = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{owner.Id}", owner);
        updateRequest.Content = JsonContent.Create(new
        {
            firstName = owner.FirstName,
            lastName = owner.LastName,
            email = owner.Email,
            gender = (int)owner.Gender,
            password = "Changed-password-123",
        });

        var updateResponse = await Client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var oldTokenRequest = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{owner.Id}", owner);
        var oldTokenResponse = await Client.SendAsync(oldTokenRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, oldTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Unsafe_requests_from_an_untrusted_origin_are_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/User/login");
        request.Headers.TryAddWithoutValidation("Origin", "https://attacker.example");
        request.Content = JsonContent.Create(new
        {
            email = "unknown@example.test",
            password = "irrelevant-password",
        });

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Auth_rate_limit_returns_too_many_requests_after_the_permit_window()
    {
        await using var rateLimitedApplication = Factory.WithWebHostBuilder(_ => { });
        using var rateLimitedClient = rateLimitedApplication.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpStatusCode lastStatus = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 21; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/User/login")
            {
                Content = JsonContent.Create(new
                {
                    email = $"rate-limit-{attempt}@example.test",
                    password = "irrelevant-password",
                })
            };

            using var response = await rateLimitedClient.SendAsync(request, TestContext.Current.CancellationToken);
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task Email_lookup_is_case_insensitive_after_registration()
    {
        var email = $"Case-{Guid.NewGuid():N}@example.test";
        const string password = "Registration-password-123";

        var registrationResponse = await Client.PostAsJsonAsync("/api/User", new
        {
            firstName = "Case",
            lastName = "Insensitive",
            email,
            password,
            gender = (int)Gender.OUTRO,
            isActive = true,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await Client.PostAsJsonAsync("/api/User/login", new
        {
            email = email.ToLowerInvariant(),
            password,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}
