#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class QuizPreviewTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuizPreviewTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Published)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Preview_returns_the_player_shape_for_every_status(QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await AddSecondQuestionAsync(quizId);
        await SetQuizStatusAsync(quizId, status);

        var response = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}/preview");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(quizId, root.GetProperty("id").GetInt32());
        Assert.True(root.TryGetProperty("title", out _));
        Assert.True(root.TryGetProperty("difficulty", out _));

        var questions = root.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(2, questions.Count);
        Assert.Equal("Capital of Portugal?", questions[0].GetProperty("description").GetString());
        Assert.Equal("Capital of France?", questions[1].GetProperty("description").GetString());
        Assert.True(questions[0].GetProperty("id").GetInt32() < questions[1].GetProperty("id").GetInt32());
        Assert.DoesNotContain("correctAnswer", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_of_a_missing_quiz_returns_not_found_with_a_message()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Get, "/api/admin/quizzes/987654/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Quiz not found.", await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Preview_does_not_record_any_score()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);

        var response = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var context = CreateContext();
        Assert.Equal(0, await context.Scores.CountAsync(score => score.QuizzId == quizId, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Published)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Preview_does_not_change_the_status_or_record_a_score(QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);

        var response = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(status, await GetQuizStatusAsync(quizId));
        await using var context = CreateContext();
        Assert.Equal(0, await context.Scores.CountAsync(score => score.QuizzId == quizId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Preview_matches_the_player_detail_of_a_published_quiz()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await AddSecondQuestionAsync(quizId);
        await SetQuizStatusAsync(quizId, QuizStatus.Published);

        var preview = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}/preview");
        using var playerRequest = CreateRequest(HttpMethod.Get, $"/api/quizzes/{quizId}", GameSphere_backend.Enums.UserRole.User, UserId);
        var player = await Client.SendAsync(playerRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(HttpStatusCode.OK, player.StatusCode);
        using var previewDoc = JsonDocument.Parse(await preview.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var playerDoc = JsonDocument.Parse(await player.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(playerDoc.RootElement.GetRawText(), previewDoc.RootElement.GetRawText());
    }

    private async Task AddSecondQuestionAsync(int quizId)
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
        {
            description = "Capital of France?",
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = new[] { "Paris", "Marseille" },
            correctAnswer = "Paris"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
