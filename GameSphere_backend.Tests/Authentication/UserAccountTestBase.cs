#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.ServicesResponses;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public abstract class UserAccountTestBase : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    protected readonly GameSphereApiFactory Factory;
    protected readonly CapturingEmailService EmailService = new();
    protected readonly StubFirebaseTokenVerifier FirebaseTokenVerifier = new();
    protected HttpClient Client = null!;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _application = null!;

    protected UserAccountTestBase(PostgreSqlFixture database)
    {
        _database = database;
        Factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        _application = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(EmailService);
                services.RemoveAll<IFirebaseTokenVerifier>();
                services.AddSingleton<IFirebaseTokenVerifier>(FirebaseTokenVerifier);
            });
        });

        Client = _application.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _application.DisposeAsync();
    }

    protected async Task<(User Owner, User Other)> SeedUsersAsync()
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = CreateUser($"owner-{suffix}@example.test", "Owner");
        var other = CreateUser($"other-{suffix}@example.test", "Other");
        context.Users.AddRange(owner, other);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (owner, other);
    }

    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    protected static User CreateUser(string email, string firstName) => new()
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

    protected static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri, User user)
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

    protected sealed class CapturingEmailService : IEmailService
    {
        public string LastBody { get; private set; } = string.Empty;

        public Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            LastBody = body;
            return Task.FromResult(true);
        }
    }

    protected sealed class StubFirebaseTokenVerifier : IFirebaseTokenVerifier
    {
        public string? ExpectedToken { get; set; }
        public FirebaseUserInfo? User { get; set; }

        public Task<FirebaseUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            var result = string.Equals(idToken, ExpectedToken, StringComparison.Ordinal)
                ? User
                : null;
            return Task.FromResult(result);
        }
    }
}
