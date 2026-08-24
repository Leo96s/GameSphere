#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

public sealed class QuizAttemptPersistenceTests : QuizAttemptTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizAttemptPersistenceTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Valid_attempt_persists_score_and_returns_only_calculated_result()
    {
        var response = await SendAttemptAsync(PublishedQuizId, ValidAnswers());
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
        var response = await SendAttemptAsync(PublishedQuizId,
        [
            new { questionId = FirstQuestionId, selectedAnswer = "Alpha" },
            new { questionId = SecondQuestionId, selectedAnswer = "Wrong second answer" }
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
    public async Task Two_valid_attempts_persist_distinct_utc_quiz_scores()
    {
        var firstResponse = await SendAttemptAsync(PublishedQuizId, ValidAnswers());
        var secondResponse = await SendAttemptAsync(PublishedQuizId, ValidAnswers());
        var scores = await ScoresForQuizAsync(PublishedQuizId);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, scores.Count);
        Assert.All(scores, score =>
        {
            Assert.Equal(PlayerId, score.UserId);
            Assert.Equal(PublishedQuizId, score.QuizzId);
            Assert.Null(score.GameId);
            Assert.Equal(DateTimeKind.Utc, score.Date.Kind);
            Assert.Equal(2f, score.Points);
        });
    }
}
