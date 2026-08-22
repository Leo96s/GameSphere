#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

public sealed class QuizCatalogTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;
    private int _publishedQuizId;
    private int _ownerId;
    private string _draftTitle = null!;

    public QuizCatalogTests(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedQuizzesAsync();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Catalog_without_authentication_returns_unauthorized()
    {
        var response = await _client.GetAsync("/api/quizzes", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_catalog_returns_only_published_quizzes()
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/quizzes");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Published quiz", body, StringComparison.Ordinal);
        Assert.DoesNotContain(_draftTitle, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Draft_quiz_detail_returns_not_found()
    {
        await using var context = CreateContext();
        var draftId = await context.Quizzs
            .Where(quiz => quiz.Title == _draftTitle)
            .Select(quiz => quiz.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/quizzes/{draftId}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Published_quiz_detail_never_exposes_the_correct_answer()
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/quizzes/{_publishedQuizId}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("correctAnswer", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Catalog_clamps_invalid_pagination_values()
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/quizzes?page=0&pageSize=0");
        var clampedResponse = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var clampedBody = await clampedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, clampedResponse.StatusCode);
        Assert.Contains("Published quiz", clampedBody, StringComparison.Ordinal);

        using var largePageRequest = CreateAuthorizedRequest(HttpMethod.Get, "/api/quizzes?page=10001&pageSize=1000");
        var largePageResponse = await _client.SendAsync(largePageRequest, TestContext.Current.CancellationToken);
        var largePageBody = await largePageResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, largePageResponse.StatusCode);
        Assert.Equal("[]", largePageBody);
    }

    private async Task SeedQuizzesAsync()
    {
        await using var context = CreateContext();
        var seedSuffix = Guid.NewGuid().ToString("N");
        var owner = new User
        {
            Email = $"quiz-owner-{seedSuffix}@example.test",
            FirstName = "Quiz",
            LastName = "Owner",
            HashedPassword = "Test-password-123",
            RegistrationDate = DateTime.UtcNow,
            Gender = Gender.OUTRO,
            isActive = true,
        };
        context.Users.Add(owner);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ownerId = owner.Id;

        var published = new Quizz
        {
            Title = "Published quiz",
            Difficulty = Difficulty.EASY,
            NumberOfQuests = 1,
            RegistrationDate = DateTime.UtcNow,
            UserId = owner.Id,
            IsPublished = true,
            Questions = new List<Question>
            {
                new()
                {
                    Description = "Published question",
                    TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                    Answers = ["Secret answer", "Wrong answer"],
                    CorrectAnswer = "Secret answer",
                }
            }
        };
        _draftTitle = $"Draft quiz {seedSuffix}";
        var draft = new Quizz
        {
            Title = _draftTitle,
            Difficulty = Difficulty.HARD,
            NumberOfQuests = 1,
            RegistrationDate = DateTime.UtcNow,
            UserId = owner.Id,
            IsPublished = false,
            Questions = new List<Question>
            {
                new()
                {
                    Description = "Draft question",
                    TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                    Answers = ["Draft answer", "Other answer"],
                    CorrectAnswer = "Draft answer",
                }
            }
        };

        context.Quizzs.AddRange(published, draft);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _publishedQuizId = published.Id;
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(_ownerId));
        return request;
    }

    private static string GenerateToken(int userId)
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

        return new AuthService(configuration).GenerateToken(userId.ToString(), "player@example.test", UserRole.User);
    }
}
