#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

/// <summary>
/// GAM-45 phase 2, AC15: an attempt and an administrative change on the same quiz are serialised by the row lock.
/// Determinism: instead of relying on sleeps, the test waits until PostgreSQL reports a backend waiting on a lock
/// (pg_stat_activity.wait_event_type = 'Lock'), which proves the attempt is blocked behind the admin's FOR UPDATE.
/// </summary>
public sealed class QuizAttemptSerializationTests : QuizAttemptTestBase, IClassFixture<PostgreSqlFixture>
{
    private static readonly TimeSpan LockWaitTimeout = TimeSpan.FromSeconds(15);

    public QuizAttemptSerializationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Attempt_started_while_admin_holds_the_lock_waits_and_returns_not_found_after_unpublish()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var adminContext = CreateContext();
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(cancellation);
        var lockedQuiz = await adminContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR UPDATE")
            .SingleAsync(cancellation);

        var attempt = SendAttemptAsync(PublishedQuizId, ValidAnswers());

        await WaitForBlockedBackendAsync();
        Assert.False(attempt.IsCompleted, "The attempt completed while the admin still held the FOR UPDATE lock.");

        lockedQuiz.Status = QuizStatus.Unpublished;
        await adminContext.SaveChangesAsync(cancellation);
        Assert.False(attempt.IsCompleted, "The attempt completed before the admin committed.");
        await adminTransaction.CommitAsync(cancellation);

        var response = await attempt.WaitAsync(LockWaitTimeout, cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await ScoreCountAsync());
    }

    [Fact]
    public async Task Attempt_started_while_admin_holds_the_lock_succeeds_after_the_admin_releases_without_changes()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var adminContext = CreateContext();
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(cancellation);
        await adminContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR UPDATE")
            .SingleAsync(cancellation);

        var attempt = SendAttemptAsync(PublishedQuizId, ValidAnswers());

        await WaitForBlockedBackendAsync();
        Assert.False(attempt.IsCompleted);
        Assert.Equal(0, await ScoreCountAsync());

        await adminTransaction.RollbackAsync(cancellation);
        var response = await attempt.WaitAsync(LockWaitTimeout, cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await ScoreCountAsync());
    }

    [Fact]
    public async Task Concurrent_attempts_on_the_same_quiz_do_not_block_each_other()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var attempts = Enumerable.Range(0, 5)
            .Select(_ => SendAttemptAsync(PublishedQuizId, ValidAnswers()))
            .ToArray();

        var responses = await Task.WhenAll(attempts).WaitAsync(LockWaitTimeout, cancellation);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(5, await ScoreCountAsync());
    }

    [Fact]
    public async Task Admin_waits_for_an_attempt_holding_the_shared_lock()
    {
        // Mirror case: while a shared lock (as taken by SubmitAttemptAsync) is held, the admin's FOR UPDATE must wait.
        var cancellation = TestContext.Current.CancellationToken;
        await using var attemptContext = CreateContext();
        await using var attemptTransaction = await attemptContext.Database.BeginTransactionAsync(cancellation);
        await attemptContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR SHARE")
            .SingleAsync(cancellation);

        await using var adminContext = CreateContext();
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(cancellation);
        var adminLock = adminContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR UPDATE")
            .SingleAsync(cancellation);

        await WaitForBlockedBackendAsync();
        Assert.False(adminLock.IsCompleted);

        await attemptTransaction.CommitAsync(cancellation);
        var quiz = await adminLock.WaitAsync(LockWaitTimeout, cancellation);
        Assert.Equal(PublishedQuizId, quiz.Id);
        await adminTransaction.RollbackAsync(cancellation);
    }

    // AC11 on a non-admin, authenticated [ApiController] endpoint.
    [Theory]
    [InlineData("{\"answers\": [")]
    [InlineData("{\"answers\": \"not-a-list\"}")]
    [InlineData("{\"answers\": [{\"questionId\": \"abc\", \"selectedAnswer\": \"Alpha\"}]}")]
    public async Task Malformed_or_wrong_type_attempt_body_returns_generic_message_without_type_names(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/quizzes/{PublishedQuizId}/attempts")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = await AuthorizationFromValidRequestAsync();

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Got {(int)response.StatusCode}: {body}");
        Assert.DoesNotContain("GameSphere_backend", body, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", body, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(["message"], document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal("The request body is invalid.", document.RootElement.GetProperty("message").GetString());
        Assert.Equal(0, await ScoreCountAsync());
    }

    // Phase 3, C18/P7: a FOR SHARE held by another transaction must not block attempts (would fail under FOR UPDATE).
    [Fact]
    public async Task Attempt_succeeds_quickly_while_another_connection_holds_FOR_SHARE_on_the_quiz()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var holderContext = CreateContext();
        await using var holderTransaction = await holderContext.Database.BeginTransactionAsync(cancellation);
        await holderContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR SHARE")
            .SingleAsync(cancellation);

        var response = await SendAttemptAsync(PublishedQuizId, ValidAnswers()).WaitAsync(TimeSpan.FromSeconds(4), cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await ScoreCountAsync());
        await holderTransaction.RollbackAsync(cancellation);
    }

    // Phase 3, C20: an admin FOR UPDATE held beyond the 5 s lock_timeout yields 409 and no Score.
    [Fact]
    public async Task Attempt_returns_conflict_and_saves_no_score_when_admin_holds_the_lock_beyond_the_lock_timeout()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var adminContext = CreateContext();
        await using var adminTransaction = await adminContext.Database.BeginTransactionAsync(cancellation);
        await adminContext.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {PublishedQuizId} FOR UPDATE")
            .SingleAsync(cancellation);

        var attempt = SendAttemptAsync(PublishedQuizId, ValidAnswers());
        await WaitForBlockedBackendAsync();

        // The lock is still held by the admin; only the server-side lock_timeout can complete the request.
        var response = await attempt.WaitAsync(TimeSpan.FromSeconds(30), cancellation);
        var body = await response.Content.ReadAsStringAsync(cancellation);

        Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"Got {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        Assert.Equal(["message"], document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal("The quiz is being updated. Please try again.", document.RootElement.GetProperty("message").GetString());
        await adminTransaction.RollbackAsync(cancellation);
        Assert.Equal(0, await ScoreCountAsync());
    }

    // Phase 3, C19: binding errors on query/route never echo the client-supplied value.
    [Theory]
    [InlineData("/api/quizzes?page=abc")]
    [InlineData("/api/quizzes?pageSize=abc")]
    [InlineData("/api/User/by-id/abc")]
    public async Task Query_or_route_binding_error_returns_generic_message_without_echoing_value(string uri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = await AuthorizationFromValidRequestAsync();

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Got {(int)response.StatusCode}: {body}");
        Assert.DoesNotContain("abc", body, StringComparison.Ordinal);
        Assert.Equal("{\"message\":\"The request is invalid.\"}", body);
    }

    private async Task<AuthenticationHeaderValue> AuthorizationFromValidRequestAsync()
    {
        // Reuse the base class token generation by capturing the header of a request it builds.
        var handler = new CapturingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };
        var original = Client;
        Client = client;
        try
        {
            await SendAttemptAsync(PublishedQuizId, ValidAnswers());
        }
        finally
        {
            Client = original;
        }

        return handler.Authorization!;
    }

    private async Task WaitForBlockedBackendAsync()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < LockWaitTimeout)
        {
            await using var context = CreateContext();
            var waiting = await context.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*)::int AS \"Value\" FROM pg_stat_activity " +
                    "WHERE datname = current_database() AND wait_event_type = 'Lock'")
                .SingleAsync(cancellation);
            if (waiting > 0)
            {
                return;
            }

            await Task.Delay(50, cancellation);
        }

        Assert.Fail("No backend was observed waiting on the quiz row lock.");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
