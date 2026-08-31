#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameSphere_backend.ServicesResponses;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class CredentialsManagementTests : UserAccountTestBase, IClassFixture<PostgreSqlFixture>
{
    public CredentialsManagementTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task ChangePassword_with_correct_current_password_allows_login_with_the_new_password()
    {
        var (owner, _) = await SeedUsersAsync();
        const string newPassword = "Updated-password-123";

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/password", owner);
        request.Content = JsonContent.Create(new { currentPassword = "Test-password-123", newPassword });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/User/login",
            new { email = owner.Email, password = newPassword },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_with_the_wrong_current_password_is_rejected()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/password", owner);
        request.Content = JsonContent.Create(new { currentPassword = "not-the-password", newPassword = "Updated-password-123" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_rejects_a_new_password_shorter_than_eight_characters()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/password", owner);
        request.Content = JsonContent.Create(new { currentPassword = "Test-password-123", newPassword = "short" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("Test-password-123", persistedUser.HashedPassword));
    }

    [Fact]
    public async Task ChangePassword_cannot_target_another_users_account()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{other.Id}/password", owner);
        request.Content = JsonContent.Create(new { currentPassword = "Test-password-123", newPassword = "Updated-password-123" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_for_a_social_only_account_requires_a_verified_firebase_token()
    {
        var socialUser = CreateUser($"social-{Guid.NewGuid():N}@example.test", "Social");
        socialUser.UID = "firebase-social-user";
        socialUser.HasLocalPassword = false;
        await using (var context = CreateContext())
        {
            context.Users.Add(socialUser);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var rejectedRequest = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{socialUser.Id}/password", socialUser);
        rejectedRequest.Content = JsonContent.Create(new { newPassword = "First-local-password-123" });
        var rejectedResponse = await Client.SendAsync(rejectedRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedResponse.StatusCode);

        FirebaseTokenVerifier.ExpectedToken = "verified-social-token";
        FirebaseTokenVerifier.User = new FirebaseUserInfo(socialUser.UID, socialUser.Email, "Social Player");

        using var acceptedRequest = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{socialUser.Id}/password", socialUser);
        acceptedRequest.Content = JsonContent.Create(new
        {
            firebaseIdToken = FirebaseTokenVerifier.ExpectedToken,
            newPassword = "First-local-password-123",
        });
        var acceptedResponse = await Client.SendAsync(acceptedRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);

        await using var verificationContext = CreateContext();
        var persistedUser = await verificationContext.Users.SingleAsync(
            candidate => candidate.Id == socialUser.Id,
            TestContext.Current.CancellationToken);
        Assert.True(persistedUser.HasLocalPassword);
    }

    [Fact]
    public async Task RequestEmailChange_then_confirm_updates_the_email_and_revokes_stale_tokens()
    {
        var (owner, _) = await SeedUsersAsync();
        var newEmail = $"new-{Guid.NewGuid():N}@example.test";

        using var requestChange = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/email/request", owner);
        requestChange.Content = JsonContent.Create(new { newEmail, currentPassword = "Test-password-123" });
        var requestResponse = await Client.SendAsync(requestChange, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);

        var codeMatch = Regex.Match(EmailService.LastBody, "\\b[0-9]{6}\\b");
        Assert.True(codeMatch.Success, "The fake email service should receive a six-digit confirmation code.");

        using var confirmChange = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/email/confirm", owner);
        confirmChange.Content = JsonContent.Create(new { code = codeMatch.Value });
        var confirmResponse = await Client.SendAsync(confirmChange, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.Equal(newEmail, persistedUser.Email);
        Assert.Null(persistedUser.PendingEmail);

        using var staleTokenRequest = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{owner.Id}", owner);
        var staleTokenResponse = await Client.SendAsync(staleTokenRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, staleTokenResponse.StatusCode);
    }

    [Fact]
    public async Task RequestEmailChange_is_rejected_when_the_new_email_is_already_in_use()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/email/request", owner);
        request.Content = JsonContent.Create(new { newEmail = other.Email, currentPassword = "Test-password-123" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestEmailChange_for_a_social_only_account_requires_a_local_password_first()
    {
        var socialUser = CreateUser($"social-{Guid.NewGuid():N}@example.test", "Social");
        socialUser.UID = "firebase-social-user";
        socialUser.HasLocalPassword = false;
        await using (var context = CreateContext())
        {
            context.Users.Add(socialUser);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        FirebaseTokenVerifier.ExpectedToken = "verified-social-token";
        FirebaseTokenVerifier.User = new FirebaseUserInfo(socialUser.UID, socialUser.Email, "Social Player");

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{socialUser.Id}/email/request", socialUser);
        request.Content = JsonContent.Create(new
        {
            newEmail = $"new-{Guid.NewGuid():N}@example.test",
            firebaseIdToken = FirebaseTokenVerifier.ExpectedToken,
        });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_with_an_invalid_code_is_rejected()
    {
        var (owner, _) = await SeedUsersAsync();

        using var requestChange = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/email/request", owner);
        requestChange.Content = JsonContent.Create(new
        {
            newEmail = $"new-{Guid.NewGuid():N}@example.test",
            currentPassword = "Test-password-123",
        });
        var requestResponse = await Client.SendAsync(requestChange, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);

        using var confirmChange = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/email/confirm", owner);
        confirmChange.Content = JsonContent.Create(new { code = "000000" });
        var confirmResponse = await Client.SendAsync(confirmChange, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, confirmResponse.StatusCode);
    }
}
