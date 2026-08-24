#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class QuizLifecycleTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizLifecycleTests(PostgreSqlFixture database)
        : base(database)
    {
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

        await AssertQuizOwnerAsync(quizId, AdminId);

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
    public async Task Question_normalizer_rejects_duplicate_options()
    {
        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Question with duplicate options",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Option A", "  Option A  " },
            correctAnswer = "Option A"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
