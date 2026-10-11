#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Controllers;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

public sealed class PublicQuestionContractTests : QuizAttemptTestBase, IClassFixture<PostgreSqlFixture>
{
    public PublicQuestionContractTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    // Note: this guard inspects declared return types only; runtime payloads are covered by the HTTP test below.
    [Fact]
    public void Non_admin_controller_return_types_never_expose_a_correct_answer_property()
    {
        var controllerTypes = typeof(QuizzesController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => !type.Name.StartsWith("Admin", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(controllerTypes, type => type == typeof(QuizzesController));

        var offenders = new List<string>();
        foreach (var controller in controllerTypes)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Visit(UnwrapTask(action.ReturnType), $"{controller.Name}.{action.Name}", new HashSet<Type>(), offenders);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public async Task Player_responses_never_contain_the_correct_answer_field_or_text()
    {
        var marker = $"UNIQUE-CORRECT-{Guid.NewGuid():N}";
        await using (var context = CreateContext())
        {
            var questions = context.Set<GameSphere_backend.Models.BackendModels.Question>()
                .Where(question => question.QuizzId == PublishedQuizId)
                .OrderBy(question => question.Id)
                .ToList();
            foreach (var question in questions)
            {
                // Options stay public by design; the correct option is one of them and carries the marker.
                var correct = marker + "-" + question.Id;
                question.Answers = [correct, "Wrong option " + question.Id];
                question.CorrectAnswer = correct;
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var list = await ReadAsync(HttpMethod.Get, "/api/quizzes?pageSize=100", null, HttpStatusCode.OK);
        var detail = await ReadAsync(HttpMethod.Get, $"/api/quizzes/{PublishedQuizId}", null, HttpStatusCode.OK);
        var result = await ReadAsync(HttpMethod.Post, $"/api/quizzes/{PublishedQuizId}/attempts", new
        {
            answers = new object[]
            {
                new { questionId = FirstQuestionId, selectedAnswer = "Wrong option " + FirstQuestionId },
                new { questionId = SecondQuestionId, selectedAnswer = "Wrong option " + SecondQuestionId }
            }
        }, HttpStatusCode.OK);
        var badRequests = new[]
        {
            await ReadAsync(HttpMethod.Post, $"/api/quizzes/{PublishedQuizId}/attempts", new
            {
                answers = new object[] { new { questionId = FirstQuestionId, selectedAnswer = "Wrong option " + FirstQuestionId } }
            }, HttpStatusCode.BadRequest),
            await ReadAsync(HttpMethod.Post, $"/api/quizzes/{PublishedQuizId}/attempts", new { answers = "invalid" }, HttpStatusCode.BadRequest)
        };
        var notFounds = new[]
        {
            await ReadAsync(HttpMethod.Get, "/api/quizzes/987654", null, HttpStatusCode.NotFound),
            await ReadAsync(HttpMethod.Get, $"/api/quizzes/{DraftQuizId}", null, HttpStatusCode.NotFound),
            await ReadAsync(HttpMethod.Post, $"/api/quizzes/{DraftQuizId}/attempts", new { answers = Array.Empty<object>() }, HttpStatusCode.NotFound)
        };

        // The detail legitimately lists the correct option text, so only the property name is checked there.
        foreach (var body in new[] { list, detail, result }.Concat(badRequests).Concat(notFounds))
        {
            AssertNoCorrectAnswerProperty(JsonDocument.Parse(body).RootElement);
        }

        foreach (var body in new[] { result }.Concat(badRequests).Concat(notFounds))
        {
            Assert.DoesNotContain(marker, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Player_quiz_controller_actions_declare_a_concrete_action_result_type()
    {
        var actions = typeof(QuizzesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToList();
        Assert.NotEmpty(actions);

        foreach (var action in actions)
        {
            var returnType = UnwrapTask(action.ReturnType);
            Assert.True(
                returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>),
                $"{action.Name} must declare ActionResult<T> so the declared-type guard covers it.");
            var payload = returnType.GetGenericArguments()[0];
            Assert.False(
                typeof(IActionResult).IsAssignableFrom(payload) || payload == typeof(object),
                $"{action.Name} must declare a concrete payload type, not {payload.Name}.");
        }
    }

    private static void AssertNoCorrectAnswerProperty(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.False(
                        string.Equals(property.Name, "correctAnswer", StringComparison.OrdinalIgnoreCase),
                        "Public response exposes a correctAnswer property.");
                    AssertNoCorrectAnswerProperty(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoCorrectAnswerProperty(item);
                }

                break;
        }
    }

    private async Task<string> ReadAsync(HttpMethod method, string uri, object? payload, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(PlayerId));
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expected, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    private static void Visit(Type type, string path, HashSet<Type> visited, List<string> offenders)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            Visit(type.GetGenericArguments()[0], path, visited, offenders);
            return;
        }

        if (type.IsArray)
        {
            Visit(type.GetElementType()!, path, visited, offenders);
            return;
        }

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                Visit(argument, path, visited, offenders);
            }

            return;
        }

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid) ||
            type.Namespace is null || type.Namespace.StartsWith("System", StringComparison.Ordinal) ||
            typeof(ActionResult).IsAssignableFrom(type) || typeof(IActionResult).IsAssignableFrom(type) ||
            !visited.Add(type))
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (string.Equals(property.Name, "CorrectAnswer", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add($"{path} -> {type.Name}.{property.Name}");
            }

            Visit(property.PropertyType, $"{path}.{property.Name}", visited, offenders);
        }
    }

    // Proves the structural guard is not vacuous: the private visitor must flag fake leaking shapes.
    [Theory]
    [InlineData(typeof(LeakyDto))]
    [InlineData(typeof(ActionResult<LeakyDto>))]
    [InlineData(typeof(ActionResult<List<LeakyDto>>))]
    [InlineData(typeof(LeakyDto[]))]
    [InlineData(typeof(WrapperDto))]
    [InlineData(typeof(ActionResult<IEnumerable<WrapperDto>>))]
    public void Reflection_guard_flags_a_correct_answer_property_in_fake_return_types(Type returnType)
    {
        var offenders = RunVisit(returnType);

        Assert.NotEmpty(offenders);
    }

    [Theory]
    [InlineData(typeof(SafeDto))]
    [InlineData(typeof(ActionResult<List<SafeDto>>))]
    public void Reflection_guard_accepts_fake_return_types_without_a_correct_answer(Type returnType)
    {
        Assert.Empty(RunVisit(returnType));
    }

    private static List<string> RunVisit(Type type)
    {
        var visit = typeof(PublicQuestionContractTests).GetMethod("Visit", BindingFlags.NonPublic | BindingFlags.Static)!;
        var offenders = new List<string>();
        visit.Invoke(null, [type, "Fake.Action", new HashSet<Type>(), offenders]);
        return offenders;
    }

    public sealed class LeakyDto
    {
        public string Description { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
    }

    public sealed class WrapperDto
    {
        public List<LeakyDto> Questions { get; set; } = [];
    }

    public sealed class SafeDto
    {
        public string Description { get; set; } = "";
        public List<string> Answers { get; set; } = [];
    }
}
