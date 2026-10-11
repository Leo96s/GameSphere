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

public sealed class QuizPublicationLifecycleTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizPublicationLifecycleTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Quiz_published_through_the_status_endpoint_is_listed_and_accepts_an_attempt()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Published)).StatusCode);

        var catalog = await SendPlayerRequestAsync(HttpMethod.Get, "/api/quizzes?pageSize=100");
        var catalogBody = await catalog.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        Assert.Contains($"\"id\":{quizId}", catalogBody, StringComparison.Ordinal);

        var attempt = await SendAttemptAsync(quizId, questionId);
        Assert.Equal(HttpStatusCode.OK, attempt.StatusCode);
        Assert.Equal(1, await ScoreCountAsync(quizId));
    }

    [Theory]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Withdrawing_or_archiving_keeps_existing_scores(QuizStatus target)
    {
        var quizId = await CreatePublishedQuizWithScoresAsync();
        var before = await ScorePointsAsync(quizId);
        Assert.Equal(2, before.Count);

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);
        if (target == QuizStatus.Archived)
        {
            Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Archived)).StatusCode);
        }

        Assert.Equal(before, await ScorePointsAsync(quizId));
    }

    [Fact]
    public async Task Restoring_an_archived_quiz_keeps_existing_scores()
    {
        var quizId = await CreatePublishedQuizWithScoresAsync();
        var before = await ScorePointsAsync(quizId);
        await SetQuizStatusAsync(quizId, QuizStatus.Archived);

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);

        Assert.Equal(before, await ScorePointsAsync(quizId));
    }

    [Fact]
    public async Task Restoring_an_archived_quiz_and_republishing_makes_it_playable_again_with_scores_intact()
    {
        var quizId = await CreatePublishedQuizWithScoresAsync();
        var questionId = await FirstQuestionIdAsync(quizId);
        var before = await ScorePointsAsync(quizId);
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Archived)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Published)).StatusCode);

        var detail = await SendPlayerRequestAsync(HttpMethod.Get, $"/api/quizzes/{quizId}");
        var attempt = await SendAttemptAsync(quizId, questionId);

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, attempt.StatusCode);
        var after = await ScorePointsAsync(quizId);
        Assert.Equal(before, after.Take(before.Count).ToList());
        Assert.Equal(before.Count + 1, after.Count);
    }

    [Theory]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Detail_and_attempt_return_a_not_found_message_after_withdrawal_and_record_nothing(QuizStatus target)
    {
        var quizId = await CreatePublishedQuizWithScoresAsync();
        var questionId = await FirstQuestionIdAsync(quizId);
        await SetQuizStatusAsync(quizId, target);
        var before = await ScorePointsAsync(quizId);

        var detail = await SendPlayerRequestAsync(HttpMethod.Get, $"/api/quizzes/{quizId}");
        var attempt = await SendAttemptAsync(quizId, questionId);

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal("Quiz not found.", await ReadErrorMessageAsync(detail));
        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        Assert.Equal("Quiz not found.", await ReadErrorMessageAsync(attempt));
        Assert.Equal(before, await ScorePointsAsync(quizId));
    }

    private async Task<int> CreatePublishedQuizWithScoresAsync()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);
        await AddAttemptAsync(quizId);
        await AddAttemptAsync(quizId);
        return quizId;
    }

    private async Task<HttpResponseMessage> SendPlayerRequestAsync(HttpMethod method, string uri, object? payload = null)
    {
        using var request = CreateRequest(method, uri, UserRole.User, UserId, payload);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> SendAttemptAsync(int quizId, int questionId) =>
        SendPlayerRequestAsync(HttpMethod.Post, $"/api/quizzes/{quizId}/attempts", new
        {
            answers = new[] { new { questionId, selectedAnswer = "Lisbon" } }
        });

    private async Task<int> ScoreCountAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Scores.CountAsync(score => score.QuizzId == quizId, TestContext.Current.CancellationToken);
    }

    private async Task<System.Collections.Generic.List<(int UserId, float Points)>> ScorePointsAsync(int quizId)
    {
        await using var context = CreateContext();
        var scores = await context.Scores
            .Where(score => score.QuizzId == quizId)
            .OrderBy(score => score.Id)
            .Select(score => new { score.UserId, score.Points })
            .ToListAsync(TestContext.Current.CancellationToken);
        return scores.Select(score => (score.UserId, score.Points)).ToList();
    }
}
