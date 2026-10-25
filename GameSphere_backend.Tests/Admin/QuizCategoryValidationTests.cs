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

public sealed class QuizCategoryValidationTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string InvalidCategory = "The quiz category is invalid.";

    public QuizCategoryValidationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Create_quiz_without_a_category_returns_bad_request()
    {
        var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            new { title = "No category", difficulty = (int)Difficulty.EASY });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidCategory, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Create_quiz_with_a_null_category_returns_bad_request()
    {
        var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            new { title = "Null category", difficulty = (int)Difficulty.EASY, category = (int?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidCategory, await ReadErrorMessageAsync(response));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Create_quiz_with_an_undefined_category_returns_bad_request(int category)
    {
        var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            new { title = "Bad category", difficulty = (int)Difficulty.EASY, category });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidCategory, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Update_quiz_without_a_category_returns_bad_request_and_keeps_the_stored_one()
    {
        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(
            HttpMethod.Put,
            $"/api/admin/quizzes/{quizId}",
            new { title = "Renamed", difficulty = (int)Difficulty.EASY });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidCategory, await ReadErrorMessageAsync(response));
        Assert.Equal(QuizCategory.GEOGRAPHY, await StoredCategoryAsync(quizId));
    }

    [Fact]
    public async Task Update_quiz_with_an_undefined_category_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(
            HttpMethod.Put,
            $"/api/admin/quizzes/{quizId}",
            new { title = "Renamed", difficulty = (int)Difficulty.EASY, category = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidCategory, await ReadErrorMessageAsync(response));
    }

    [Theory]
    [InlineData(QuizCategory.GENERAL_KNOWLEDGE)]
    [InlineData(QuizCategory.VIDEO_GAMES)]
    public async Task Create_and_update_with_a_valid_category_persist_it_and_return_it_on_get(QuizCategory category)
    {
        var createResponse = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            new { title = $"Categorised {Guid.NewGuid():N}", difficulty = (int)Difficulty.EASY, category = (int)category });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var quizId = await ReadCategoryAsync(createResponse, expected: category);

        var other = category == QuizCategory.VIDEO_GAMES ? QuizCategory.SPORTS : QuizCategory.SCIENCE;
        var updateResponse = await SendAdminRequestAsync(
            HttpMethod.Put,
            $"/api/admin/quizzes/{quizId}",
            new { title = $"Updated {Guid.NewGuid():N}", difficulty = (int)Difficulty.EASY, category = (int)other });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        await ReadCategoryAsync(updateResponse, expected: other);

        var getResponse = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        await ReadCategoryAsync(getResponse, expected: other);
        Assert.Equal(other, await StoredCategoryAsync(quizId));
    }

    [Fact]
    public async Task Publishing_a_quiz_with_an_undefined_stored_category_is_rejected()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await using (var context = CreateContext())
        {
            var quiz = await context.Quizzs.SingleAsync(candidate => candidate.Id == quizId, TestContext.Current.CancellationToken);
            quiz.Category = (QuizCategory)99;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await ChangeStatusAsync(quizId, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(InvalidCategory, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    private async Task<QuizCategory> StoredCategoryAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Quizzs
            .Where(quiz => quiz.Id == quizId)
            .Select(quiz => quiz.Category)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> ReadCategoryAsync(HttpResponseMessage response, QuizCategory expected)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        Assert.Equal((int)expected, document.RootElement.GetProperty("category").GetInt32());
        return document.RootElement.GetProperty("id").GetInt32();
    }
}
