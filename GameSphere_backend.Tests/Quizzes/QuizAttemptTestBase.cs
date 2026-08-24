#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public abstract class QuizAttemptTestBase : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    protected HttpClient Client = null!;
    protected int PlayerId;
    protected int PublishedQuizId;
    protected int FirstQuestionId;
    protected int SecondQuestionId;
    protected int DraftQuizId;
    protected int EmptyPublishedQuizId;

    protected QuizAttemptTestBase(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedQuizAsync();
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

    protected object[] ValidAnswers() =>
    [
        new { questionId = FirstQuestionId, selectedAnswer = "Alpha" },
        new { questionId = SecondQuestionId, selectedAnswer = "Bravo" }
    ];

    protected async Task<HttpResponseMessage> SendAttemptAsync(int quizId, object[] answers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/quizzes/{quizId}/attempts")
        {
            Content = JsonContent.Create(new { answers })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(PlayerId));

        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    protected async Task<int> ScoreCountAsync()
    {
        await using var context = CreateContext();
        return await context.Scores.CountAsync(score =>
            score.UserId == PlayerId && score.QuizzId == PublishedQuizId,
            TestContext.Current.CancellationToken);
    }

    protected async Task<float> ScorePointsAsync()
    {
        await using var context = CreateContext();
        return await context.Scores
            .Where(score => score.UserId == PlayerId && score.QuizzId == PublishedQuizId)
            .Select(score => score.Points)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    protected async Task<List<Score>> ScoresForQuizAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Scores
            .Where(score => score.UserId == PlayerId && score.QuizzId == quizId)
            .OrderBy(score => score.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task SeedQuizAsync()
    {
        await using var context = CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var owner = CreateUser($"owner-{suffix}@example.test");
        var player = CreateUser($"player-{suffix}@example.test");
        context.Users.AddRange(owner, player);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        PlayerId = player.Id;

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
        PublishedQuizId = publishedQuiz.Id;
        DraftQuizId = draftQuiz.Id;
        EmptyPublishedQuizId = emptyPublishedQuiz.Id;
        FirstQuestionId = publishedQuiz.Questions!.ElementAt(0).Id;
        SecondQuestionId = publishedQuiz.Questions.ElementAt(1).Id;
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
