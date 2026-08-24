#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public abstract class AdminQuizAdministrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    protected HttpClient Client = null!;
    protected int AdminId;
    protected int InactiveAdminId;
    protected int UserId;

    protected AdminQuizAdministrationTestBase(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedUsersAsync();
        Client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected async Task<int> CreateQuizAsync()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = $"Empty quiz {Guid.NewGuid():N}",
            difficulty = (int)Difficulty.EASY,
            isPublished = false
        });
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("id").GetInt32();
    }

    protected async Task<HttpResponseMessage> SendAdminRequestAsync(HttpMethod method, string uri, object? payload = null)
    {
        using var request = CreateRequest(method, uri, UserRole.Admin, payload: payload);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    protected HttpRequestMessage CreateRequest(
        HttpMethod method,
        string uri,
        UserRole role,
        int? userId = null,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId ?? AdminId, role));
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return request;
    }

    protected async Task AssertQuizOwnerAsync(int quizId, int adminId)
    {
        await using var context = CreateContext();
        var ownerId = await context.Quizzs
            .Where(quiz => quiz.Id == quizId)
            .Select(quiz => quiz.UserId)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(adminId, ownerId);
    }

    protected async Task AssertQuizQuestionCountAsync(int quizId, int expectedCount)
    {
        await using var context = CreateContext();
        var quiz = await context.Quizzs
            .Include(candidate => candidate.Questions)
            .SingleAsync(candidate => candidate.Id == quizId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCount, quiz.NumberOfQuests);
        Assert.Equal(expectedCount, quiz.Questions?.Count ?? 0);
    }

    protected async Task AssertQuizDoesNotExistAsync(int quizId)
    {
        await using var context = CreateContext();
        Assert.False(await context.Quizzs.AnyAsync(quiz => quiz.Id == quizId, TestContext.Current.CancellationToken));
    }

    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task SeedUsersAsync()
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var admin = new User
        {
            Email = $"admin-{suffix}@example.test",
            FirstName = "Admin",
            LastName = "Test",
            HashedPassword = "Test-password-123",
            RegistrationDate = DateTime.UtcNow,
            Gender = Gender.OUTRO,
            isActive = true,
            Role = UserRole.Admin
        };
        var inactiveAdmin = new User
        {
            Email = $"inactive-admin-{suffix}@example.test",
            FirstName = "Inactive",
            LastName = "Admin",
            HashedPassword = "Test-password-123",
            RegistrationDate = DateTime.UtcNow,
            Gender = Gender.OUTRO,
            isActive = false,
            Role = UserRole.Admin
        };
        var user = new User
        {
            Email = $"user-{suffix}@example.test",
            FirstName = "User",
            LastName = "Test",
            HashedPassword = "Test-password-123",
            RegistrationDate = DateTime.UtcNow,
            Gender = Gender.OUTRO,
            isActive = true,
            Role = UserRole.User
        };
        context.Users.AddRange(admin, inactiveAdmin, user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        AdminId = admin.Id;
        InactiveAdminId = inactiveAdmin.Id;
        UserId = user.Id;
    }

    private static string GenerateToken(int userId, UserRole role)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-with-at-least-thirty-two-bytes",
                ["JwtSettings:Issuer"] = "gamesphere-tests",
                ["JwtSettings:Audience"] = "gamesphere-tests",
                ["JwtSettings:ExpirationMinutes"] = "60"
            })
            .Build();

        return new AuthService(configuration).GenerateToken(
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "admin@example.test",
            role);
    }
}
