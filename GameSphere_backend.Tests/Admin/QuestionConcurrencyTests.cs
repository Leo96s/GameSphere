#nullable enable

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class QuestionConcurrencyTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public QuestionConcurrencyTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Concurrent_question_changes_keep_the_quiz_question_count_consistent()
    {
        var quizId = await CreateQuizAsync();

        var creates = Enumerable.Range(1, 12)
            .Select(index => SendAdminRequestAsync(HttpMethod.Post, $"/api/admin/quizzes/{quizId}/questions", new
            {
                description = $"Concurrent question {index}",
                typeOfAnswer = (int)TypeOfAnswer.MULTIPLEOPTION,
                answers = new[] { $"Answer {index}", $"Alternative {index}" },
                correctAnswer = $"Answer {index}"
            }));

        var responses = await Task.WhenAll(creates);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        await AssertQuizQuestionCountAsync(quizId, 12);
    }
}
