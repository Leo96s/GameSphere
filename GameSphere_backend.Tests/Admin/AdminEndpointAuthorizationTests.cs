#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class AdminEndpointAuthorizationTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public AdminEndpointAuthorizationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    public static IEnumerable<object[]> AdminEndpoints()
    {
        yield return ["GET", "/api/admin/quizzes", false];
        yield return ["GET", "/api/admin/quizzes/{quiz}", false];
        yield return ["GET", "/api/admin/quizzes/{quiz}/preview", false];
        yield return ["POST", "/api/admin/quizzes", true];
        yield return ["PUT", "/api/admin/quizzes/{quiz}", true];
        yield return ["DELETE", "/api/admin/quizzes/{quiz}", false];
        yield return ["POST", "/api/admin/quizzes/{quiz}/status", true];
        yield return ["POST", "/api/admin/quizzes/{quiz}/questions", true];
        yield return ["PUT", "/api/admin/questions/{question}", true];
        yield return ["DELETE", "/api/admin/questions/{question}", false];
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoint_without_a_token_returns_unauthorized_and_changes_nothing(
        string method,
        string template,
        bool hasBody)
    {
        var (quizId, questionId) = await SeedAsync();
        var before = await SnapshotAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), Resolve(template, quizId, questionId));
        AttachBody(request, hasBody);

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoint_with_a_user_role_returns_forbidden_and_changes_nothing(
        string method,
        string template,
        bool hasBody)
    {
        var (quizId, questionId) = await SeedAsync();
        var before = await SnapshotAsync();
        using var request = CreateRequest(
            new HttpMethod(method),
            Resolve(template, quizId, questionId),
            UserRole.User,
            UserId,
            hasBody ? Payload() : null);

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoint_with_an_inactive_admin_returns_forbidden_and_changes_nothing(
        string method,
        string template,
        bool hasBody)
    {
        var (quizId, questionId) = await SeedAsync();
        var before = await SnapshotAsync();
        using var request = CreateRequest(
            new HttpMethod(method),
            Resolve(template, quizId, questionId),
            UserRole.Admin,
            InactiveAdminId,
            hasBody ? Payload() : null);

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    private async Task<(int QuizId, int QuestionId)> SeedAsync()
    {
        var quizId = await CreateQuizWithQuestionAsync();
        return (quizId, await FirstQuestionIdAsync(quizId));
    }

    private static string Resolve(string template, int quizId, int questionId) => template
        .Replace("{quiz}", quizId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace("{question}", questionId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static void AttachBody(HttpRequestMessage request, bool hasBody)
    {
        if (hasBody)
        {
            request.Content = System.Net.Http.Json.JsonContent.Create(Payload());
        }
    }

    private static object Payload() => new
    {
        title = "Unauthorized change",
        difficulty = (int)Difficulty.HARD,
        status = (int)QuizStatus.Published,
        description = "Unauthorized question?",
        typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
        answers = new[] { "One", "Two" },
        correctAnswer = "One"
    };

    private async Task<string> SnapshotAsync()
    {
        await using var context = CreateContext();
        var quizzes = await context.Quizzs
            .Include(quiz => quiz.Questions)
            .OrderBy(quiz => quiz.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        return string.Join(
            "|",
            quizzes.Select(quiz =>
                $"{quiz.Id}:{quiz.Title}:{quiz.Difficulty}:{quiz.Status}:{quiz.NumberOfQuests}:" +
                string.Join(",", (quiz.Questions ?? []).OrderBy(question => question.Id)
                    .Select(question => $"{question.Id}={question.Description}/{question.CorrectAnswer}"))));
    }
}
