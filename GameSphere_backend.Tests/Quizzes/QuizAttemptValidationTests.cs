#nullable enable

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

public sealed class QuizAttemptValidationTests : QuizAttemptTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizAttemptValidationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Attempt_with_duplicate_question_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(PublishedQuizId,
        [
            new { questionId = FirstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = FirstQuestionId, selectedAnswer = "Alpha" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_missing_question_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(PublishedQuizId,
        [new { questionId = FirstQuestionId, selectedAnswer = "Alpha" }]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_validator_rejects_unknown_question_ids()
    {
        var response = await SendAttemptAsync(PublishedQuizId,
        [
            new { questionId = FirstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = 987654, selectedAnswer = "Unexpected" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_only_unknown_question_id_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(PublishedQuizId,
        [
            new { questionId = 123456789, selectedAnswer = "Ghost answer" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_with_invalid_answer_option_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(PublishedQuizId,
        [
            new { questionId = FirstQuestionId, selectedAnswer = "Invalid option" },
            new { questionId = SecondQuestionId, selectedAnswer = "Bravo" }
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_without_authentication_returns_unauthorized()
    {
        var response = await Client.PostAsJsonAsync($"/api/quizzes/{PublishedQuizId}/attempts", new
        {
            answers = ValidAnswers()
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_for_unpublished_quiz_returns_not_found()
    {
        var response = await SendAttemptAsync(DraftQuizId,
        [new { questionId = FirstQuestionId, selectedAnswer = "Alpha" }]);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_for_published_quiz_without_questions_returns_bad_request_without_persisting_score()
    {
        var response = await SendAttemptAsync(EmptyPublishedQuizId, []);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ScoresForQuizAsync(EmptyPublishedQuizId));
    }
}
