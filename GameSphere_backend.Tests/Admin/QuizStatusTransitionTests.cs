#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class QuizStatusTransitionTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizStatusTransitionTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    public static TheoryData<QuizStatus, QuizStatus> AllowedTransitions() => new()
    {
        { QuizStatus.Draft, QuizStatus.Published },
        { QuizStatus.Unpublished, QuizStatus.Published },
        { QuizStatus.Published, QuizStatus.Unpublished },
        { QuizStatus.Draft, QuizStatus.Archived },
        { QuizStatus.Unpublished, QuizStatus.Archived },
        { QuizStatus.Archived, QuizStatus.Unpublished }
    };

    public static TheoryData<QuizStatus, QuizStatus> ForbiddenTransitions()
    {
        var allowed = AllowedTransitions().Select(row => (row.Data.Item1, row.Data.Item2)).ToHashSet();
        var data = new TheoryData<QuizStatus, QuizStatus>();
        foreach (var from in Enum.GetValues<QuizStatus>())
        {
            foreach (var to in Enum.GetValues<QuizStatus>())
            {
                if (!allowed.Contains((from, to)))
                {
                    data.Add(from, to);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public async Task Allowed_transition_returns_ok_with_the_new_status(QuizStatus from, QuizStatus to)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, from);

        var response = await ChangeStatusAsync(quizId, to);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(quizId, document.RootElement.GetProperty("id").GetInt32());
        Assert.Equal((int)to, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(to, await GetQuizStatusAsync(quizId));
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public async Task Forbidden_transition_returns_conflict_with_message_and_keeps_status(QuizStatus from, QuizStatus to)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, from);

        var response = await ChangeStatusAsync(quizId, to);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal($"Cannot change quiz status from {from} to {to}.", await ReadErrorMessageAsync(response));
        Assert.Equal(from, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Full_editorial_cycle_through_the_status_endpoint()
    {
        var quizId = await CreateQuizWithQuestionAsync();

        foreach (var status in new[] { QuizStatus.Published, QuizStatus.Unpublished, QuizStatus.Archived, QuizStatus.Unpublished, QuizStatus.Published })
        {
            var response = await ChangeStatusAsync(quizId, status);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(status, await GetQuizStatusAsync(quizId));
        }
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    public async Task Publishing_a_quiz_without_questions_returns_bad_request_with_message(QuizStatus from)
    {
        var quizId = await CreateQuizAsync();
        await SetQuizStatusAsync(quizId, from);

        var response = await ChangeStatusAsync(quizId, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A quiz must have at least one question to be published.", await ReadErrorMessageAsync(response));
        Assert.Equal(from, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Publishing_a_quiz_with_an_invalid_question_reports_its_number()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await using (var context = CreateContext())
        {
            // Persisted directly: the admin API would refuse this question, but legacy data may contain it.
            context.Set<Question>().Add(new Question
            {
                Description = "Broken question",
                TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
                Answers = ["Option A", "Option B"],
                CorrectAnswer = "Option C",
                QuizzId = quizId
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await ChangeStatusAsync(quizId, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Question 2: The correct answer must be one of the question options.",
            await ReadErrorMessageAsync(response));
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Publishing_a_true_or_false_question_with_three_options_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();
        var created = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "The sky is blue.",
            typeOfAnswer = (int)TypeOfAnswer.TRUEORFALSE,
            answers = new[] { "True", "False", "Maybe" },
            correctAnswer = "True"
        });
        if (created.StatusCode != HttpStatusCode.Created)
        {
            await using var context = CreateContext();
            context.Set<Question>().Add(new Question
            {
                Description = "The sky is blue.",
                TypeOfAnswer = TypeOfAnswer.TRUEORFALSE,
                Answers = ["True", "False", "Maybe"],
                CorrectAnswer = "True",
                QuizzId = quizId
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await ChangeStatusAsync(quizId, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Question 1: A true or false question must have exactly two options.",
            await ReadErrorMessageAsync(response));
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Undefined_status_returns_bad_request_with_message(int status)
    {
        var quizId = await CreateQuizWithQuestionAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/status", new { status });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Changing_the_status_of_a_missing_quiz_returns_not_found_with_message()
    {
        var response = await ChangeStatusAsync(int.MaxValue, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Quiz not found.", await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Status_endpoint_without_a_token_returns_unauthorized()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/status")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { status = (int)QuizStatus.Published })
        };

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Status_endpoint_with_a_user_role_returns_forbidden()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/admin/quizzes/{quizId}/status",
            UserRole.User,
            UserId,
            new { status = (int)QuizStatus.Published });

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Status_endpoint_with_an_inactive_admin_returns_forbidden()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/admin/quizzes/{quizId}/status",
            UserRole.Admin,
            InactiveAdminId,
            new { status = (int)QuizStatus.Published });

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task New_quiz_always_starts_as_draft_even_if_a_status_is_sent()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = $"Sneaky quiz {Guid.NewGuid():N}",
            difficulty = (int)Difficulty.EASY, category = (int)QuizCategory.GEOGRAPHY,
            status = (int)QuizStatus.Published,
            isPublished = true,
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
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var quizId = document.RootElement.GetProperty("id").GetInt32();
        Assert.Equal((int)QuizStatus.Draft, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    [Theory]
    [InlineData(QuizStatus.Draft, QuizStatus.Published)]
    [InlineData(QuizStatus.Draft, QuizStatus.Archived)]
    [InlineData(QuizStatus.Unpublished, QuizStatus.Published)]
    public async Task Update_ignores_a_status_in_the_body(QuizStatus current, QuizStatus sent)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, current);

        var response = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", new
        {
            title = "Renamed quiz",
            difficulty = (int)Difficulty.HARD, category = (int)QuizCategory.GEOGRAPHY,
            status = (int)sent,
            isPublished = true
        });
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal((int)current, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(current, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Admin_list_hides_archived_quizzes_unless_requested()
    {
        var archivedId = await CreateQuizWithQuestionAsync();
        var draftId = await CreateQuizWithQuestionAsync();
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(archivedId, QuizStatus.Archived)).StatusCode);

        var defaultIds = await ListAdminQuizIdsAsync("/api/admin/quizzes?pageSize=100");
        var explicitFalseIds = await ListAdminQuizIdsAsync("/api/admin/quizzes?pageSize=100&includeArchived=false");
        var includedIds = await ListAdminQuizIdsAsync("/api/admin/quizzes?pageSize=100&includeArchived=true");

        Assert.Contains(draftId, defaultIds);
        Assert.DoesNotContain(archivedId, defaultIds);
        Assert.DoesNotContain(archivedId, explicitFalseIds);
        Assert.Contains(archivedId, includedIds);
        Assert.Contains(draftId, includedIds);
    }

    [Fact]
    public async Task Admin_quiz_detail_returns_the_status()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Archived);

        var response = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal((int)QuizStatus.Archived, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("questions").GetArrayLength());
    }

    [Fact]
    public async Task Admin_quiz_detail_for_a_missing_quiz_returns_not_found_with_message()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    private async Task<List<int>> ListAdminQuizIdsAsync(string uri)
    {
        var response = await SendAdminRequestAsync(HttpMethod.Get, uri);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.EnumerateArray().Select(quiz => quiz.GetProperty("id").GetInt32()).ToList();
    }
}
