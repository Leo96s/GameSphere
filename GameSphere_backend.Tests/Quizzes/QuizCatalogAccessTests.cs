#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

public sealed class QuizCatalogAccessTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;
    private int _publishedQuizId;
    private int _ownerId;
    private string _draftTitle = null!;
    private int _draftQuizId;
    private readonly Dictionary<QuizStatus, (int QuizId, int QuestionId, string Title)> _hiddenQuizzes = new();

    public QuizCatalogAccessTests(PostgreSqlFixture database)
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

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Catalog_list_excludes_non_published_quizzes(QuizStatus status)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/quizzes?pageSize=100");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Published quiz", body, StringComparison.Ordinal);
        Assert.DoesNotContain(_hiddenQuizzes[status].Title, body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Catalog_detail_of_a_non_published_quiz_returns_not_found(QuizStatus status)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/quizzes/{_hiddenQuizzes[status].QuizId}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Attempt_on_a_non_published_quiz_returns_not_found_without_persisting_a_score(QuizStatus status)
    {
        var hidden = _hiddenQuizzes[status];
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/quizzes/{hidden.QuizId}/attempts");
        request.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            answers = new[] { new { questionId = hidden.QuestionId, selectedAnswer = "Hidden answer" } }
        });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var context = CreateContext();
        Assert.False(await context.Scores.AnyAsync(score => score.QuizzId == hidden.QuizId, TestContext.Current.CancellationToken));
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
            Status = QuizStatus.Published,
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
            Status = QuizStatus.Draft,
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

        var unpublished = HiddenQuiz(owner.Id, QuizStatus.Unpublished, $"Unpublished quiz {seedSuffix}");
        var archived = HiddenQuiz(owner.Id, QuizStatus.Archived, $"Archived quiz {seedSuffix}");

        context.Quizzs.AddRange(published, draft, unpublished, archived);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _publishedQuizId = published.Id;
        _draftQuizId = draft.Id;
        _hiddenQuizzes[QuizStatus.Draft] = (draft.Id, draft.Questions!.First().Id, draft.Title);
        _hiddenQuizzes[QuizStatus.Unpublished] = (unpublished.Id, unpublished.Questions!.First().Id, unpublished.Title);
        _hiddenQuizzes[QuizStatus.Archived] = (archived.Id, archived.Questions!.First().Id, archived.Title);
    }

    private static Quizz HiddenQuiz(int ownerId, QuizStatus status, string title) => new()
    {
        Title = title,
        Difficulty = Difficulty.MEDIUM,
        NumberOfQuests = 1,
        RegistrationDate = DateTime.UtcNow,
        UserId = ownerId,
        Status = status,
        Questions = new List<Question>
        {
            new()
            {
                Description = $"{status} question",
                TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                Answers = ["Hidden answer", "Other answer"],
                CorrectAnswer = "Hidden answer",
            }
        }
    };


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
