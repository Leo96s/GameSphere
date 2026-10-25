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

public sealed class QuizDraftValidationTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string UnsupportedCharacters = "The text contains unsupported control or invisible characters.";

    public QuizDraftValidationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Create_quiz_with_a_whitespace_title_returns_bad_request()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", QuizPayload("     "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Create_quiz_with_an_undefined_difficulty_returns_bad_request()
    {
        var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            new { title = "Valid title", difficulty = 99, category = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Update_quiz_with_a_whitespace_title_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", QuizPayload("     "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Create_question_with_a_description_of_2001_characters_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(description: new string('A', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Create_question_with_a_description_of_2000_characters_is_accepted()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(description: new string('A', 2000)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_question_with_one_option_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(answers: ["Only"], correctAnswer: "Only"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Create_question_with_seven_options_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();
        var options = Enumerable.Range(1, 7).Select(index => $"Option {index}").ToArray();

        var response = await PostQuestionAsync(quizId, QuestionPayload(answers: options, correctAnswer: "Option 1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ReadErrorMessageAsync(response)));
    }

    [Fact]
    public async Task Create_question_with_six_options_is_accepted()
    {
        var quizId = await CreateQuizAsync();
        var options = Enumerable.Range(1, 6).Select(index => $"Option {index}").ToArray();

        var response = await PostQuestionAsync(quizId, QuestionPayload(answers: options, correctAnswer: "Option 1"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_question_with_options_that_differ_only_by_case_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(answers: ["Paris", "paris"], correctAnswer: "Paris"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each question option must be unique and non-empty.", await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Update_question_with_options_that_differ_only_by_case_returns_bad_request()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        var response = await PutQuestionAsync(questionId, QuestionPayload(answers: ["Paris", "paris"], correctAnswer: "Paris"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each question option must be unique and non-empty.", await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Update_question_rejects_the_same_size_and_option_rules()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        var longDescription = await PutQuestionAsync(questionId, QuestionPayload(description: new string('A', 2001)));
        var oneOption = await PutQuestionAsync(questionId, QuestionPayload(answers: ["Only"], correctAnswer: "Only"));
        var sevenOptions = await PutQuestionAsync(questionId, QuestionPayload(
            answers: Enumerable.Range(1, 7).Select(index => $"Option {index}").ToArray(),
            correctAnswer: "Option 1"));
        var missingCorrect = await PutQuestionAsync(questionId, QuestionPayload(correctAnswer: "Missing"));

        Assert.Equal(HttpStatusCode.BadRequest, longDescription.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, oneOption.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sevenOptions.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCorrect.StatusCode);
    }

    [Fact]
    public async Task Create_question_with_a_padded_correct_answer_is_accepted_and_normalized()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(
            answers: ["  Lisbon  ", "Porto"],
            correctAnswer: "  Lisbon  "));
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Lisbon", document.RootElement.GetProperty("correctAnswer").GetString());
        Assert.Equal("Lisbon", document.RootElement.GetProperty("answers")[0].GetString());
    }

    [Theory]
    [InlineData("\u0007")]
    [InlineData("​")]
    [InlineData("‮")]
    [InlineData("⁦")]
    [InlineData("﻿")]
    public async Task Create_quiz_with_forbidden_characters_in_the_title_returns_bad_request(string forbidden)
    {
        var response = await SendAdminRequestAsync(
            HttpMethod.Post,
            "/api/admin/quizzes",
            QuizPayload($"Quiz{forbidden}title"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
    }

    [Theory]
    [InlineData("\u0007")]
    [InlineData("​")]
    [InlineData("‮")]
    [InlineData("⁦")]
    [InlineData("﻿")]
    public async Task Create_question_with_forbidden_characters_returns_bad_request_in_every_text_field(string forbidden)
    {
        var quizId = await CreateQuizAsync();

        var description = await PostQuestionAsync(quizId, QuestionPayload(description: $"Question{forbidden}?"));
        var option = await PostQuestionAsync(quizId, QuestionPayload(answers: [$"Lis{forbidden}bon", "Porto"], correctAnswer: "Porto"));
        var correct = await PostQuestionAsync(quizId, QuestionPayload(
            answers: ["Lisbon", "Porto"],
            correctAnswer: $"Lis{forbidden}bon"));

        foreach (var response in new[] { description, option, correct })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
        }
    }

    [Fact]
    public async Task Update_question_with_forbidden_characters_returns_bad_request()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        var response = await PutQuestionAsync(questionId, QuestionPayload(description: "Question‮?"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Create_question_accepts_line_breaks_in_the_description_and_zero_width_joiner()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(
            description: "Line one\nLine two <b>bold</b>",
            answers: ["\U0001F468‍\U0001F469", "Porto"],
            correctAnswer: "Porto"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_quiz_rejects_a_line_break_in_the_title()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", QuizPayload("Line one\nLine two"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Create_question_rejects_a_line_break_in_an_option()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(answers: ["Lis\nbon", "Porto"], correctAnswer: "Porto"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Draft_quiz_without_questions_is_saved_but_hidden_from_the_public_catalog()
    {
        var created = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", QuizPayload($"Empty draft {Guid.NewGuid():N}"));
        var createdBody = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var document = JsonDocument.Parse(createdBody);
        var quizId = document.RootElement.GetProperty("id").GetInt32();
        var title = document.RootElement.GetProperty("title").GetString()!;
        Assert.Equal((int)QuizStatus.Draft, document.RootElement.GetProperty("status").GetInt32());

        using var listRequest = CreateRequest(HttpMethod.Get, "/api/quizzes?pageSize=100", UserRole.User, UserId);
        var list = await Client.SendAsync(listRequest, TestContext.Current.CancellationToken);
        var listBody = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var detailRequest = CreateRequest(HttpMethod.Get, $"/api/quizzes/{quizId}", UserRole.User, UserId);
        var detail = await Client.SendAsync(detailRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.DoesNotContain(title, listBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.True(await QuizExistsAsync(quizId));
    }

    private async Task<bool> QuizExistsAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Quizzs.AnyAsync(quiz => quiz.Id == quizId, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostQuestionAsync(int quizId, object payload) =>
        SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", payload);

    private Task<HttpResponseMessage> PutQuestionAsync(int questionId, object payload) =>
        SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/questions/{questionId}", payload);

    private static object QuizPayload(string title) => new { title, difficulty = (int)Difficulty.EASY, category = (int)QuizCategory.GEOGRAPHY };

    private static object QuestionPayload(
        string description = "Capital of Portugal?",
        string[]? answers = null,
        string correctAnswer = "Lisbon") => new
        {
            description,
            typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
            answers = answers ?? ["Lisbon", "Porto"],
            correctAnswer
        };

    [Theory]
    [InlineData("\u0007")]
    [InlineData("⁣")]
    [InlineData("‮")]
    public async Task Update_quiz_with_forbidden_characters_in_the_title_returns_bad_request_and_keeps_the_title(string forbidden)
    {
        var quizId = await CreateQuizAsync();
        var before = await TitleAsync(quizId);

        var response = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", QuizPayload($"Quiz{forbidden}title"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
        Assert.Equal(before, await TitleAsync(quizId));
    }

    [Theory]
    [InlineData("\u0000")]
    [InlineData("‏")]
    [InlineData("⁩")]
    public async Task Update_question_with_forbidden_characters_in_options_or_correct_answer_returns_bad_request(string forbidden)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        var option = await PutQuestionAsync(questionId, QuestionPayload(answers: [$"Lis{forbidden}bon", "Porto"], correctAnswer: "Porto"));
        var correct = await PutQuestionAsync(questionId, QuestionPayload(answers: ["Lisbon", "Porto"], correctAnswer: $"Lis{forbidden}bon"));

        foreach (var response in new[] { option, correct })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(UnsupportedCharacters, await ReadErrorMessageAsync(response));
        }
    }

    [Fact]
    public async Task Update_question_accepts_html_line_breaks_in_the_description_and_zero_width_joiner()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        var questionId = await FirstQuestionIdAsync(quizId);

        var response = await PutQuestionAsync(questionId, QuestionPayload(
            description: "Line one\r\n\tLine <script>alert(1)</script>",
            answers: ["\U0001F468‍\U0001F469", "<b>Porto</b>"],
            correctAnswer: "<b>Porto</b>"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_question_with_a_forbidden_character_does_not_persist_anything()
    {
        var quizId = await CreateQuizAsync();

        var response = await PostQuestionAsync(quizId, QuestionPayload(description: "Bad⁧?"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var context = CreateContext();
        Assert.False(await context.Set<GameSphere_backend.Models.BackendModels.Question>()
            .AnyAsync(question => question.QuizzId == quizId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Publishing_a_draft_with_a_stored_forbidden_character_returns_bad_request_and_stays_draft()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await using (var context = CreateContext())
        {
            var question = await context.Set<GameSphere_backend.Models.BackendModels.Question>()
                .FirstAsync(candidate => candidate.QuizzId == quizId, TestContext.Current.CancellationToken);
            question.Description = "Capital of Portugal?\u202E";
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await ChangeStatusAsync(quizId, QuizStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Question 1: " + UnsupportedCharacters, await ReadErrorMessageAsync(response));
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    private async Task<string> TitleAsync(int quizId)
    {
        await using var context = CreateContext();
        return (await context.Quizzs.SingleAsync(quiz => quiz.Id == quizId, TestContext.Current.CancellationToken)).Title;
    }
}
