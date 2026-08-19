#nullable enable

using System;
using System.Collections.Generic;
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

namespace GameSphere_backend.Tests.Admin;

public sealed class QuizAdministrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;
    private int _adminId;
    private int _inactiveAdminId;
    private int _userId;

    public QuizAdministrationTests(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedUsersAsync();
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
    public async Task Administration_without_a_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/api/admin/quizzes", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administration_with_a_user_role_returns_forbidden()
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/admin/quizzes", UserRole.User, _userId);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administration_with_an_inactive_admin_session_returns_forbidden_for_reads_and_writes()
    {
        using var readRequest = CreateRequest(HttpMethod.Get, "/api/admin/quizzes", UserRole.Admin, _inactiveAdminId);
        using var writeRequest = CreateRequest(HttpMethod.Post, "/api/admin/quizzes", UserRole.Admin, _inactiveAdminId, new
        {
            title = "Inactive admin quiz",
            difficulty = (int)Difficulty.EASY,
            isPublished = false
        });

        var readResponse = await _client.SendAsync(readRequest, TestContext.Current.CancellationToken);
        var writeResponse = await _client.SendAsync(writeRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_can_manage_a_quiz_and_its_questions()
    {
        var createdQuiz = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = "Curated geography quiz",
            difficulty = (int)Difficulty.EASY,
            isPublished = false,
            questions = new[]
            {
                new
                {
                    description = "Capital of Portugal?",
                    typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
                    answers = new[] { "Lisbon", "Porto" },
                    correctAnswer = "Lisbon"
                }
            }
        });
        var createdBody = await createdQuiz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createdQuiz.StatusCode);
        using var createdDocument = JsonDocument.Parse(createdBody);
        var quizId = createdDocument.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(1, createdDocument.RootElement.GetProperty("numberOfQuests").GetInt32());
        Assert.False(createdDocument.RootElement.GetProperty("isPublished").GetBoolean());

        await AssertQuizOwnerAsync(quizId, _adminId);

        var updatedQuiz = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", new
        {
            title = "Published geography quiz",
            difficulty = (int)Difficulty.MEDIUM,
            isPublished = true,
            questions = new[]
            {
                new
                {
                    description = "Capital of Portugal?",
                    typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
                    answers = new[] { "Lisbon", "Porto" },
                    correctAnswer = "Lisbon"
                },
                new
                {
                    description = "Capital of Spain?",
                    typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
                    answers = new[] { "Madrid", "Barcelona" },
                    correctAnswer = "Madrid"
                }
            }
        });
        var updatedBody = await updatedQuiz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updatedQuiz.StatusCode);
        using var updatedDocument = JsonDocument.Parse(updatedBody);
        Assert.True(updatedDocument.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.Equal(2, updatedDocument.RootElement.GetProperty("numberOfQuests").GetInt32());

        var listResponse = await SendAdminRequestAsync(HttpMethod.Get, "/api/admin/quizzes");
        var listBody = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains("Published geography quiz", listBody, StringComparison.Ordinal);

        var createdQuestion = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Capital of France?",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Paris", "Lyon" },
            correctAnswer = "Paris"
        });
        var questionBody = await createdQuestion.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createdQuestion.StatusCode);
        using var questionDocument = JsonDocument.Parse(questionBody);
        var questionId = questionDocument.RootElement.GetProperty("id").GetInt32();
        await AssertQuizQuestionCountAsync(quizId, 3);

        var updatedQuestion = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/questions/{questionId}", new
        {
            description = "Capital of France?",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Paris", "Marseille" },
            correctAnswer = "Paris"
        });

        Assert.Equal(HttpStatusCode.OK, updatedQuestion.StatusCode);

        var deletedQuestion = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/questions/{questionId}");

        Assert.Equal(HttpStatusCode.NoContent, deletedQuestion.StatusCode);
        await AssertQuizQuestionCountAsync(quizId, 2);

        var unpublishedQuiz = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", new
        {
            title = "Published geography quiz",
            difficulty = (int)Difficulty.MEDIUM,
            isPublished = false
        });
        var unpublishedBody = await unpublishedQuiz.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, unpublishedQuiz.StatusCode);
        using var unpublishedDocument = JsonDocument.Parse(unpublishedBody);
        Assert.False(unpublishedDocument.RootElement.GetProperty("isPublished").GetBoolean());

        var deletedQuiz = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.NoContent, deletedQuiz.StatusCode);
        await AssertQuizDoesNotExistAsync(quizId);
    }

    [Fact]
    public async Task Invalid_question_is_rejected_without_changing_the_quiz_count()
    {
        var invalidQuiz = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = " ",
            difficulty = (int)Difficulty.EASY,
            isPublished = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidQuiz.StatusCode);

        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Invalid question",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Option A", "Option B" },
            correctAnswer = "Option C"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertQuizQuestionCountAsync(quizId, 0);

        var unsupportedType = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Unsupported answer type",
            typeOfAnswer = 99,
            answers = new[] { "Option A", "Option B" },
            correctAnswer = "Option A"
        });

        Assert.Equal(HttpStatusCode.BadRequest, unsupportedType.StatusCode);
        await AssertQuizQuestionCountAsync(quizId, 0);
    }

    [Fact]
    public async Task Quiz_with_existing_attempts_cannot_be_deleted()
    {
        var quizId = await CreateQuizAsync();
        await using (var context = CreateContext())
        {
            var player = new User
            {
                Email = $"player-{Guid.NewGuid():N}@example.test",
                FirstName = "Test",
                LastName = "Player",
                HashedPassword = "Test-password-123",
                RegistrationDate = DateTime.UtcNow,
                Gender = Gender.OUTRO,
                isActive = true,
                Role = UserRole.User
            };
            context.Users.Add(player);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Scores.Add(new Score
            {
                UserId = player.Id,
                QuizzId = quizId,
                GameId = null,
                Points = 1,
                Date = DateTime.UtcNow
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertQuizQuestionCountAsync(quizId, 0);
    }

    [Fact]
    public async Task Concurrent_question_changes_keep_the_quiz_question_count_consistent()
    {
        var quizId = await CreateQuizAsync();

        var creates = Enumerable.Range(1, 12)
            .Select(index => SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
            {
                description = $"Concurrent question {index}",
                typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
                answers = new[] { $"Answer {index}", $"Alternative {index}" },
                correctAnswer = $"Answer {index}"
            }));

        var responses = await Task.WhenAll(creates);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        await AssertQuizQuestionCountAsync(quizId, 12);
    }

    private async Task<int> CreateQuizAsync()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = $"Empty quiz {Guid.NewGuid():N}",
            difficulty = (int)Difficulty.EASY,
            isPublished = false
        });
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<HttpResponseMessage> SendAdminRequestAsync(HttpMethod method, string uri, object? payload = null)
    {
        using var request = CreateRequest(method, uri, UserRole.Admin, payload: payload);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string uri,
        UserRole role,
        int? userId = null,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId ?? _adminId, role));
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return request;
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
        _adminId = admin.Id;
        _inactiveAdminId = inactiveAdmin.Id;
        _userId = user.Id;
    }

    private async Task AssertQuizOwnerAsync(int quizId, int adminId)
    {
        await using var context = CreateContext();
        var ownerId = await context.Quizzs
            .Where(quiz => quiz.Id == quizId)
            .Select(quiz => quiz.UserId)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(adminId, ownerId);
    }

    private async Task AssertQuizQuestionCountAsync(int quizId, int expectedCount)
    {
        await using var context = CreateContext();
        var quiz = await context.Quizzs
            .Include(candidate => candidate.Questions)
            .SingleAsync(candidate => candidate.Id == quizId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCount, quiz.NumberOfQuests);
        Assert.Equal(expectedCount, quiz.Questions?.Count ?? 0);
    }

    private async Task AssertQuizDoesNotExistAsync(int quizId)
    {
        await using var context = CreateContext();
        Assert.False(await context.Quizzs.AnyAsync(quiz => quiz.Id == quizId, TestContext.Current.CancellationToken));
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
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
