#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class QuizContentProtectionTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string NotEditableMessage = "Only draft or unpublished quizzes can be edited.";
    private const string ExistingAttemptsMessage = "A quiz with existing attempts cannot have its content changed.";

    public QuizContentProtectionTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    public enum ContentOperation
    {
        CreateQuestion,
        UpdateQuestion,
        DeleteQuestion,
        UpdateQuizTitle,
        UpdateQuizQuestions
    }

    public static TheoryData<ContentOperation, QuizStatus> LockedStatuses()
    {
        var data = new TheoryData<ContentOperation, QuizStatus>();
        foreach (var operation in Enum.GetValues<ContentOperation>())
        {
            data.Add(operation, QuizStatus.Published);
            data.Add(operation, QuizStatus.Archived);
        }

        return data;
    }

    public static TheoryData<ContentOperation, QuizStatus> EditableStatuses()
    {
        var data = new TheoryData<ContentOperation, QuizStatus>();
        foreach (var operation in Enum.GetValues<ContentOperation>())
        {
            data.Add(operation, QuizStatus.Draft);
            data.Add(operation, QuizStatus.Unpublished);
        }

        return data;
    }

    public static TheoryData<ContentOperation> AllOperations()
    {
        var data = new TheoryData<ContentOperation>();
        foreach (var operation in Enum.GetValues<ContentOperation>())
        {
            data.Add(operation);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LockedStatuses))]
    public async Task Content_of_a_published_or_archived_quiz_cannot_be_changed(ContentOperation operation, QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);
        var before = await SnapshotAsync(quizId);

        var response = await ExecuteAsync(operation, quizId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(NotEditableMessage, await ReadErrorMessageAsync(response));
        Assert.Equal(before, await SnapshotAsync(quizId));
        Assert.Equal(status, await GetQuizStatusAsync(quizId));
    }

    [Theory]
    [MemberData(nameof(EditableStatuses))]
    public async Task Content_of_a_quiz_with_attempts_cannot_be_changed(ContentOperation operation, QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);
        await AddAttemptAsync(quizId);
        var before = await SnapshotAsync(quizId);

        var response = await ExecuteAsync(operation, quizId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ExistingAttemptsMessage, await ReadErrorMessageAsync(response));
        Assert.Equal(before, await SnapshotAsync(quizId));
    }

    [Theory]
    [MemberData(nameof(AllOperations))]
    public async Task Content_of_a_published_quiz_with_attempts_cannot_be_changed(ContentOperation operation)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);
        await AddAttemptAsync(quizId);
        var before = await SnapshotAsync(quizId);

        var response = await ExecuteAsync(operation, quizId);

        // The plan does not define which of the two rules wins when both apply; only the 409 is asserted.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(await ReadErrorMessageAsync(response), new[] { NotEditableMessage, ExistingAttemptsMessage });
        Assert.Equal(before, await SnapshotAsync(quizId));
    }

    [Theory]
    [MemberData(nameof(EditableStatuses))]
    public async Task Content_of_a_draft_or_unpublished_quiz_without_attempts_can_be_changed(ContentOperation operation, QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);

        var response = await ExecuteAsync(operation, quizId);

        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {(int)response.StatusCode}.");
        Assert.Equal(operation switch
        {
            ContentOperation.CreateQuestion => HttpStatusCode.Created,
            ContentOperation.DeleteQuestion => HttpStatusCode.NoContent,
            _ => HttpStatusCode.OK
        }, response.StatusCode);
        Assert.Equal(status, await GetQuizStatusAsync(quizId));
    }

    [Fact]
    public async Task Archived_then_restored_quiz_becomes_editable_again()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Archived)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ExecuteAsync(ContentOperation.CreateQuestion, quizId)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await ExecuteAsync(ContentOperation.CreateQuestion, quizId)).StatusCode);
        await AssertQuizQuestionCountAsync(quizId, 2);
    }

    [Fact]
    public async Task Status_changes_are_still_allowed_for_a_quiz_with_attempts()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);
        await AddAttemptAsync(quizId);

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Archived)).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_quiz_with_attempts_still_returns_conflict()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Unpublished);
        await AddAttemptAsync(quizId);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
        await AssertQuizQuestionCountAsync(quizId, 1);
    }

    [Fact]
    public async Task Editing_a_missing_question_returns_not_found_with_message()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/questions/{int.MaxValue}", ValidQuestionPayload("Missing"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Invalid_question_returns_bad_request_with_message()
    {
        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Invalid question",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Option A", "Option B" },
            correctAnswer = "Option C"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The correct answer must be one of the question options.", await ReadErrorMessageAsync(response));
    }

    private async Task<HttpResponseMessage> ExecuteAsync(ContentOperation operation, int quizId)
    {
        var questionId = await FirstQuestionIdAsync(quizId);
        return operation switch
        {
            ContentOperation.CreateQuestion => await SendAdminRequestAsync(
                HttpMethod.Post,
                $"/api/admin/quizzes/{quizId}/questions",
                ValidQuestionPayload("Capital of Spain?")),
            ContentOperation.UpdateQuestion => await SendAdminRequestAsync(
                HttpMethod.Put,
                $"/api/admin/questions/{questionId}",
                ValidQuestionPayload("Capital of Portugal (edited)?")),
            ContentOperation.DeleteQuestion => await SendAdminRequestAsync(
                HttpMethod.Delete,
                $"/api/admin/questions/{questionId}"),
            ContentOperation.UpdateQuizTitle => await SendAdminRequestAsync(
                HttpMethod.Put,
                $"/api/admin/quizzes/{quizId}",
                new { title = "Edited title", difficulty = (int)Difficulty.HARD }),
            ContentOperation.UpdateQuizQuestions => await SendAdminRequestAsync(
                HttpMethod.Put,
                $"/api/admin/quizzes/{quizId}",
                new
                {
                    title = "Edited title",
                    difficulty = (int)Difficulty.HARD,
                    questions = new[] { ValidQuestionPayload("Replaced question?") }
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static object ValidQuestionPayload(string description) => new
    {
        description,
        typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
        answers = new[] { "Madrid", "Barcelona" },
        correctAnswer = "Madrid"
    };

    private async Task<string> SnapshotAsync(int quizId)
    {
        await using var context = CreateContext();
        var quiz = await context.Quizzs
            .AsNoTracking()
            .Include(candidate => candidate.Questions)
            .SingleAsync(candidate => candidate.Id == quizId, TestContext.Current.CancellationToken);
        var questions = (quiz.Questions ?? [])
            .OrderBy(question => question.Id)
            .Select(question => $"{question.Id}|{question.Description}|{question.TypeOfAnswer}|{string.Join(",", question.Answers)}|{question.CorrectAnswer}");

        return $"{quiz.Title}|{quiz.Difficulty}|{quiz.NumberOfQuests}|{string.Join(";", questions)}";
    }
}
