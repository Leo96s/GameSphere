#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

/// <summary>
/// GAM-45 phase 2: acceptance criteria 11 (uniform { message } 400s), 12 (status payload required),
/// 13 (Published quizzes cannot be deleted) and 14 (hasAttempts on the admin DTO).
/// </summary>
public sealed class QuizEditorialPhaseTwoTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string InvalidBodyMessage = "The request body is invalid.";
    private const string InvalidStatusMessage = "A valid quiz status is required.";
    private const string DeletePublishedMessage = "Unpublish the quiz before deleting it.";

    public QuizEditorialPhaseTwoTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    // ---------- AC11 ----------

    // Phase 3, C19: admin query binding error does not echo the value.
    [Theory]
    [InlineData("/api/admin/quizzes?page=abc")]
    [InlineData("/api/admin/quizzes?includeArchived=abc")]
    public async Task Admin_query_binding_error_returns_generic_message_without_echoing_value(string uri)
    {
        var response = await SendAdminRequestAsync(HttpMethod.Get, uri);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Got {(int)response.StatusCode}: {body}");
        Assert.DoesNotContain("abc", body, StringComparison.Ordinal);
        Assert.Equal("{\"message\":\"The request is invalid.\"}", body);
    }

    public static TheoryData<string, string, string> InvalidAdminBodies() => new()
    {
        { "POST", "/api/admin/quizzes", "{\"title\": \"Broken" },
        { "POST", "/api/admin/quizzes", "{\"title\": 123, \"difficulty\": 0}" },
        { "POST", "/api/admin/quizzes", "{\"title\": \"Valid\", \"difficulty\": \"not-a-number\"}" },
        { "POST", "/api/admin/quizzes", "{\"title\": \"Valid\", \"difficulty\": 0, \"questions\": {}}" },
        { "STATUS", "", "{\"status\": " },
        { "STATUS", "", "{\"status\": \"Bogus\"}" },
        { "STATUS", "", "{\"status\": true}" },
        { "STATUS", "", "[1, 2]" }
    };

    [Theory]
    [MemberData(nameof(InvalidAdminBodies))]
    public async Task Malformed_or_wrong_type_json_on_admin_endpoint_returns_generic_message(string method, string uri, string json)
    {
        if (method == "STATUS")
        {
            var quizId = await CreateQuizWithQuestionAsync();
            method = "POST";
            uri = $"/api/admin/quizzes/{quizId}/status";
        }

        var response = await SendRawAdminAsync(new HttpMethod(method), uri, json);

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, InvalidBodyMessage);
    }

    [Fact]
    public async Task Malformed_json_on_admin_update_returns_generic_message_and_keeps_quiz()
    {
        var quizId = await CreateQuizWithQuestionAsync();

        var response = await SendRawAdminAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", "{ not json");

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, InvalidBodyMessage);
        await AssertQuizQuestionCountAsync(quizId, 1);
    }

    [Fact]
    public async Task Data_annotation_failure_on_admin_endpoint_returns_the_annotation_message()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = new string('x', 101),
            difficulty = (int)Difficulty.EASY
        });

        await AssertOnlyMessageAsync(
            response,
            HttpStatusCode.BadRequest,
            "The field Title must be a string with a maximum length of 100.");
    }

    [Fact]
    public async Task Malformed_json_on_public_endpoint_returns_generic_message()
    {
        var response = await SendRawAnonymousAsync("/api/User/validate-reset-code", "{\"email\": ");

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, InvalidBodyMessage);
    }

    [Fact]
    public async Task Wrong_type_json_on_public_endpoint_returns_generic_message()
    {
        var response = await SendRawAnonymousAsync(
            "/api/User/validate-reset-code",
            "{\"email\": 42, \"resetCode\": \"123456\"}");

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, InvalidBodyMessage);
    }

    [Fact]
    public async Task Data_annotation_failure_on_public_endpoint_returns_the_annotation_message()
    {
        var response = await SendRawAnonymousAsync(
            "/api/User/validate-reset-code",
            "{\"email\": \"someone@example.test\", \"resetCode\": \"abc\"}");

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, "Reset code must be six digits");
    }

    // ---------- AC12 ----------

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"status\": null}")]
    [InlineData("{\"status\": 99}")]
    [InlineData("{\"status\": -1}")]
    public async Task Status_request_without_a_valid_status_returns_bad_request_and_keeps_status(string json)
    {
        var quizId = await CreateQuizWithQuestionAsync();

        var response = await SendRawAdminAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/status", json);

        await AssertOnlyMessageAsync(response, HttpStatusCode.BadRequest, InvalidStatusMessage);
        Assert.Equal(QuizStatus.Draft, await GetQuizStatusAsync(quizId));
    }

    // ---------- AC13 ----------

    [Fact]
    public async Task Deleting_a_published_quiz_returns_conflict_and_keeps_it()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        await AssertOnlyMessageAsync(response, HttpStatusCode.Conflict, DeletePublishedMessage);
        Assert.Equal(QuizStatus.Published, await GetQuizStatusAsync(quizId));
        await AssertQuizQuestionCountAsync(quizId, 1);
    }

    [Fact]
    public async Task Deleting_a_published_quiz_through_the_api_flow_returns_conflict()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Published)).StatusCode);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(DeletePublishedMessage, await ReadErrorMessageAsync(response));

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(quizId, QuizStatus.Unpublished)).StatusCode);
        var afterUnpublish = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.NoContent, afterUnpublish.StatusCode);
        await AssertQuizDoesNotExistAsync(quizId);
    }

    [Fact]
    public async Task Deleting_a_published_quiz_with_attempts_returns_conflict_and_keeps_it()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, QuizStatus.Published);
        await AddAttemptAsync(quizId);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(DeletePublishedMessage, await ReadErrorMessageAsync(response));
        Assert.Equal(QuizStatus.Published, await GetQuizStatusAsync(quizId));
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Deleting_a_non_published_quiz_without_attempts_returns_no_content(QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertQuizDoesNotExistAsync(quizId);
    }

    [Theory]
    [InlineData(QuizStatus.Draft)]
    [InlineData(QuizStatus.Unpublished)]
    [InlineData(QuizStatus.Archived)]
    public async Task Deleting_a_non_published_quiz_with_attempts_still_returns_conflict(QuizStatus status)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        await SetQuizStatusAsync(quizId, status);
        await AddAttemptAsync(quizId);

        var response = await SendAdminRequestAsync(HttpMethod.Delete, $"/api/admin/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("A quiz with existing attempts cannot be deleted.", await ReadErrorMessageAsync(response));
        Assert.Equal(status, await GetQuizStatusAsync(quizId));
    }

    // ---------- AC14 ----------

    [Fact]
    public async Task Create_response_reports_no_attempts_and_ignores_a_client_supplied_flag()
    {
        var response = await SendAdminRequestAsync(HttpMethod.Post, "/api/admin/quizzes", new
        {
            title = $"Flag quiz {Guid.NewGuid():N}",
            difficulty = (int)Difficulty.EASY,
            hasAttempts = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.False(document.RootElement.GetProperty("hasAttempts").GetBoolean());
    }

    [Fact]
    public async Task Update_response_reports_has_attempts()
    {
        var quizId = await CreateQuizWithQuestionAsync();

        var response = await SendAdminRequestAsync(HttpMethod.Put, $"/api/admin/quizzes/{quizId}", new
        {
            title = $"Renamed {Guid.NewGuid():N}",
            difficulty = (int)Difficulty.EASY,
            hasAttempts = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.False(document.RootElement.GetProperty("hasAttempts").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_list_and_status_responses_report_has_attempts(bool withAttempts)
    {
        var quizId = await CreateQuizWithQuestionAsync();
        if (withAttempts)
        {
            await AddAttemptAsync(quizId);
        }

        var detail = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using (var document = await ReadJsonAsync(detail))
        {
            Assert.Equal(withAttempts, document.RootElement.GetProperty("hasAttempts").GetBoolean());
        }

        Assert.Equal(withAttempts, await FindHasAttemptsInListAsync(quizId));

        var status = await ChangeStatusAsync(quizId, QuizStatus.Published);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        using (var document = await ReadJsonAsync(status))
        {
            Assert.Equal(withAttempts, document.RootElement.GetProperty("hasAttempts").GetBoolean());
        }
    }

    [Fact]
    public async Task List_reports_has_attempts_per_quiz_including_archived()
    {
        var withAttempts = await CreateQuizWithQuestionAsync();
        var withoutAttempts = await CreateQuizWithQuestionAsync();
        var archivedWithAttempts = await CreateQuizWithQuestionAsync();
        await AddAttemptAsync(withAttempts);
        await AddAttemptAsync(withAttempts);
        await AddAttemptAsync(archivedWithAttempts);
        await SetQuizStatusAsync(archivedWithAttempts, QuizStatus.Archived);

        Assert.True(await FindHasAttemptsInListAsync(withAttempts));
        Assert.False(await FindHasAttemptsInListAsync(withoutAttempts));
        Assert.True(await FindHasAttemptsInListAsync(archivedWithAttempts));
    }

    // ---------- helpers ----------

    private async Task<bool?> FindHasAttemptsInListAsync(int quizId)
    {
        for (var page = 1; page <= 50; page++)
        {
            var response = await SendAdminRequestAsync(
                HttpMethod.Get,
                $"/api/admin/quizzes?includeArchived=true&pageSize=100&page={page}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = await ReadJsonAsync(response);
            var items = document.RootElement.EnumerateArray().ToList();
            if (items.Count == 0)
            {
                break;
            }

            var match = items.FirstOrDefault(item => item.GetProperty("id").GetInt32() == quizId);
            if (match.ValueKind != JsonValueKind.Undefined)
            {
                return match.GetProperty("hasAttempts").GetBoolean();
            }
        }

        Assert.Fail($"Quiz {quizId} was not found in the admin list.");
        return null;
    }

    private async Task<HttpResponseMessage> SendRawAdminAsync(HttpMethod method, string uri, string json)
    {
        using var request = CreateRequest(method, uri, UserRole.Admin);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> SendRawAnonymousAsync(string uri, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body);
    }

    internal static async Task AssertOnlyMessageAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedMessage)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(expectedStatus == response.StatusCode, $"Expected {(int)expectedStatus}, got {(int)response.StatusCode}: {body}");
        AssertNoInternalTypeNames(body);

        using var document = JsonDocument.Parse(body);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(["message"], properties);
        Assert.Equal(expectedMessage, document.RootElement.GetProperty("message").GetString());
    }

    internal static void AssertNoInternalTypeNames(string body)
    {
        Assert.DoesNotContain("GameSphere_backend", body, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"errors\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"traceId\"", body, StringComparison.Ordinal);
    }
}
