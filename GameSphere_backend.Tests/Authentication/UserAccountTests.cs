#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class UserAccountTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private readonly CapturingEmailService _emailService = new();
    private HttpClient _client = null!;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _application = null!;

    public UserAccountTests(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        _application = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(_emailService);
            });
        });

        _client = _application.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task User_cannot_read_another_user_by_id()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{other.Id}", owner);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_read_another_user_by_email()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/User/by-email/{Uri.EscapeDataString(other.Email)}",
            owner);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_update_another_user()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{other.Id}", owner);
        request.Content = JsonContent.Create(new
        {
            firstName = "Unauthorized",
            lastName = "Update",
            email = other.Email,
            gender = (int)Gender.OUTRO,
            isActive = true,
        });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == other.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Other", persistedUser.FirstName);
    }

    [Fact]
    public async Task User_cannot_delete_another_user()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"/api/User/{other.Id}", owner);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = CreateContext();
        Assert.True(await context.Users.AnyAsync(user => user.Id == other.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task User_can_read_and_update_own_profile()
    {
        var (owner, _) = await SeedUsersAsync();

        using (var getRequest = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{owner.Id}", owner))
        {
            var getResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        using var updateRequest = CreateAuthorizedRequest(HttpMethod.Put, $"/api/User/{owner.Id}", owner);
        updateRequest.Content = JsonContent.Create(new
        {
            firstName = "Updated",
            lastName = "Owner",
            email = owner.Email,
            gender = (int)Gender.FEMININO,
            isActive = true,
        });

        var updateResponse = await _client.SendAsync(updateRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Updated", persistedUser.FirstName);
        Assert.Equal(Gender.FEMININO, persistedUser.Gender);
    }

    [Fact]
    public async Task User_can_delete_own_account()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"/api/User/{owner.Id}", owner);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = CreateContext();
        Assert.False(await context.Users.AnyAsync(user => user.Id == owner.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Registration_and_login_create_a_valid_authenticated_session()
    {
        var email = $"registration-{Guid.NewGuid():N}@example.test";
        const string password = "Registration-password-123";

        var registrationResponse = await _client.PostAsJsonAsync("/api/User", new
        {
            firstName = "New",
            lastName = "Player",
            email,
            hashedPassword = password,
            gender = (int)Gender.OUTRO,
            isActive = true,
        }, TestContext.Current.CancellationToken);

        var registrationBody = await registrationResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(registrationResponse.StatusCode == HttpStatusCode.Created, registrationBody);

        var loginResponse = await _client.PostAsJsonAsync("/api/User/login", new
        {
            email,
            password,
        }, TestContext.Current.CancellationToken);
        var loginBody = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginDocument = JsonDocument.Parse(loginBody);
        Assert.False(string.IsNullOrWhiteSpace(loginDocument.RootElement.GetProperty("token").GetString()));
        Assert.DoesNotContain("hashedPassword", loginBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Password_recovery_accepts_the_email_code_and_new_password()
    {
        var (_, user) = await SeedUsersAsync();
        const string newPassword = "Recovered-password-123";

        var sendResponse = await _client.PostAsJsonAsync(
            "/api/User/send-reset-code",
            user.Email,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var resetCodeMatch = Regex.Match(_emailService.LastBody, "\\b[0-9]{6}\\b");
        Assert.True(resetCodeMatch.Success, "The fake email service should receive a six-digit reset code.");
        var resetCode = resetCodeMatch.Value;

        var validationResponse = await _client.PostAsJsonAsync(
            "/api/User/validate-reset-code",
            new { email = user.Email, resetCode },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);

        var resetResponse = await _client.PostAsJsonAsync(
            "/api/User/reset-password",
            new { email = user.Email, resetCode, newPassword },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/User/login",
            new { email = user.Email, password = newPassword },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private async Task<(User Owner, User Other)> SeedUsersAsync()
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = CreateUser($"owner-{suffix}@example.test", "Owner");
        var other = CreateUser($"other-{suffix}@example.test", "Other");
        context.Users.AddRange(owner, other);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (owner, other);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateUser(string email, string firstName) => new()
    {
        Email = email,
        FirstName = firstName,
        LastName = "User",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword("Test-password-123", 4),
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true,
        Role = UserRole.User,
    };

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri, User user)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(user));
        return request;
    }

    private static string GenerateToken(User user)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-with-at-least-thirty-two-bytes",
                ["JwtSettings:Issuer"] = "gamesphere-tests",
                ["JwtSettings:Audience"] = "gamesphere-tests",
                ["JwtSettings:ExpirationMinutes"] = "60",
            })
            .Build();

        return new AuthService(configuration).GenerateToken(
            user.Id.ToString(),
            user.Email,
            user.Role);
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public string LastBody { get; private set; } = string.Empty;

        public Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            LastBody = body;
            return Task.FromResult(true);
        }
    }
}
