#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
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

namespace GameSphere_backend.Tests.Quizzes;

public sealed class QuizAttemptTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;
    private int _playerId;
    private int _publishedQuizId;
    private int _firstQuestionId;
    private int _secondQuestionId;
    private int _draftQuizId;
    private int _emptyPublishedQuizId;

    public QuizAttemptTests(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedQuizAsync();
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
    public async Task Valid_attempt_persists_score_and_returns_only_calculated_result()
    {
        var response = await SendAttemptAsync(_publishedQuizId, ValidAnswers());
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(body);
        Assert.Equal(
            ["correctAnswers", "totalQuestions", "percentage"],
            result.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(2, result.RootElement.GetProperty("correctAnswers").GetInt32());
        Assert.Equal(2, result.RootElement.GetProperty("totalQuestions").GetInt32());
        Assert.Equal(100m, result.RootElement.GetProperty("percentage").GetDecimal());
        Assert.DoesNotContain("Alpha", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Bravo", body, StringComparison.Ordinal);
        Assert.Equal(1, await ScoreCountAsync());
        Assert.Equal(2f, await ScorePointsAsync());
    }

    [Fact]
    public async Task Valid_partial_attempt_returns_calculated_percentage()
    {
        var response = await SendAttemptAsync(_publishedQuizId,
        [
            new { questionId = _firstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = _secondQuestionId, selectedAnswer = "Wrong second answer" }
        ]);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(body);
        Assert.Equal(1, result.RootElement.GetProperty("correctAnswers").GetInt32());
        Assert.Equal(50m, result.RootElement.GetProperty("percentage").GetDecimal());
        Assert.Equal(1, await ScoreCountAsync());
        Assert.Equal(1f, await ScorePointsAsync());
    }

    [Fact]
    public async Task Attempt_with_duplicate_question_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(_publishedQuizId,
        [
            new { questionId = _firstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = _firstQuestionId, selectedAnswer = "Alpha" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_missing_question_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(_publishedQuizId,
        [new { questionId = _firstQuestionId, selectedAnswer = "Alpha" }]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_unknown_question_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(_publishedQuizId,
        [
            new { questionId = _firstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = 987654, selectedAnswer = "Unexpected" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_invalid_answer_option_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(_publishedQuizId,
        [
            new { questionId = _firstQuestionId, selectedAnswer = "Invalid option" },
            new { questionId = _secondQuestionId, selectedAnswer = "Bravo" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_without_authentication_returns_unauthorized()
    {
        var response = await _client.PostAsJsonAsync($"/api/quizzes/{_publishedQuizId}/attempts", new
        {
            answers = ValidAnswers()
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_for_unpublished_quiz_returns_not_found()
    {
        var response = await SendAttemptAsync(_draftQuizId,
        [new { questionId = _firstQuestionId, selectedAnswer = "Alpha" }]);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_for_published_quiz_without_questions_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(_emptyPublishedQuizId, []);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ScoresForQuizAsync(_emptyPublishedQuizId));
    }

    [Fact]
    public async Task Two_valid_attempts_persist_distinct_utc_quiz_scores()
    {
        var firstResponse = await SendAttemptAsync(_publishedQuizId, ValidAnswers());
        var secondResponse = await SendAttemptAsync(_publishedQuizId, ValidAnswers());
        var scores = await ScoresForQuizAsync(_publishedQuizId);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, scores.Count);
        Assert.All(scores, score =>
        {
            Assert.Equal(_playerId, score.UserId);
            Assert.Equal(_publishedQuizId, score.QuizzId);
            Assert.Null(score.GameId);
            Assert.Equal(DateTimeKind.Utc, score.Date.Kind);
            Assert.Equal(2f, score.Points);
        });
    }

    private object[] ValidAnswers() =>
    [
        new { questionId = _firstQuestionId, selectedAnswer = "Alpha" },
        new { questionId = _secondQuestionId, selectedAnswer = "Bravo" }
    ];

    private async Task<HttpResponseMessage> SendAttemptAsync(int quizId, object[] answers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/quizzes/{quizId}/attempts")
        {
            Content = JsonContent.Create(new { answers })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(_playerId));

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task SeedQuizAsync()
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = CreateUser($"owner-{suffix}@example.test");
        var player = CreateUser($"player-{suffix}@example.test");
        context.Users.AddRange(owner, player);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _playerId = player.Id;

        var publishedQuiz = new Quizz
        {
            Title = $"Attempt quiz {suffix}",
            Difficulty = Difficulty.EASY,
            NumberOfQuests = 2,
            RegistrationDate = DateTime.UtcNow,
            UserId = owner.Id,
            IsPublished = true,
            Questions = new List<Question>
            {
                new()
                {
                    Description = "First question",
                    TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                    Answers = ["Alpha", "Wrong first answer"],
                    CorrectAnswer = "Alpha"
                },
                new()
                {
                    Description = "Second question",
                    TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                    Answers = ["Bravo", "Wrong second answer"],
                    CorrectAnswer = "Bravo"
                }
            }
        };
        var draftQuiz = new Quizz
        {
            Title = $"Draft attempt quiz {suffix}",
            Difficulty = Difficulty.EASY,
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
                    Answers = ["Draft answer", "Wrong draft answer"],
                    CorrectAnswer = "Draft answer"
                }
            }
        };
        var emptyPublishedQuiz = new Quizz
        {
            Title = $"Empty attempt quiz {suffix}",
            Difficulty = Difficulty.EASY,
            NumberOfQuests = 0,
            RegistrationDate = DateTime.UtcNow,
            UserId = owner.Id,
            IsPublished = true,
            Questions = []
        };

        context.Quizzs.AddRange(publishedQuiz, draftQuiz, emptyPublishedQuiz);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _publishedQuizId = publishedQuiz.Id;
        _draftQuizId = draftQuiz.Id;
        _emptyPublishedQuizId = emptyPublishedQuiz.Id;
        _firstQuestionId = publishedQuiz.Questions!.ElementAt(0).Id;
        _secondQuestionId = publishedQuiz.Questions.ElementAt(1).Id;
    }

    private async Task<int> ScoreCountAsync()
    {
        await using var context = CreateContext();
        return await context.Scores.CountAsync(score =>
            score.UserId == _playerId && score.QuizzId == _publishedQuizId,
            TestContext.Current.CancellationToken);
    }

    private async Task<float> ScorePointsAsync()
    {
        await using var context = CreateContext();
        return await context.Scores
            .Where(score => score.UserId == _playerId && score.QuizzId == _publishedQuizId)
            .Select(score => score.Points)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<Score>> ScoresForQuizAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Scores
            .Where(score => score.UserId == _playerId && score.QuizzId == quizId)
            .OrderBy(score => score.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser(string email) => new()
    {
        Email = email,
        FirstName = "Test",
        LastName = "Player",
        HashedPassword = "Test-password-123",
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true
    };

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

        return new AuthService(configuration).GenerateToken(
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "player@example.test",
            UserRole.User);
    }
}
