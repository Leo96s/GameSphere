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

public sealed class QuizContentKeyTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string InvalidKey = "The quiz content key is invalid.";
    private const string DuplicateKey = "A quiz with this content key already exists.";
    private const string ImmutableKey = "The quiz content key cannot be changed.";

    public QuizContentKeyTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Theory]
    [InlineData("Maiusculas")]
    [InlineData("com espaco")]
    [InlineData("-comeca-com-hifen")]
    [InlineData("termina-com-hifen-")]
    [InlineData("duplo--hifen")]
    [InlineData("sublinhado_aqui")]
    [InlineData("acentuação")]
    [InlineData("capitais\n")]
    [InlineData("\ncapitais")]
    [InlineData("")]
    public async Task Create_with_an_invalid_content_key_returns_bad_request(string contentKey)
    {
        var response = await CreateAsync($"Invalid {Guid.NewGuid():N}", contentKey);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidKey, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Create_with_a_content_key_longer_than_100_characters_returns_bad_request()
    {
        var response = await CreateAsync($"Long {Guid.NewGuid():N}", new string('a', 101));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidKey, await ReadErrorMessageAsync(response));
    }

    [Fact]
    public async Task Create_with_a_content_key_of_exactly_100_characters_succeeds()
    {
        var response = await CreateAsync($"Max {Guid.NewGuid():N}", new string('a', 100));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_a_valid_content_key_persists_it_and_get_returns_it()
    {
        var key = NewKey();
        var response = await CreateAsync($"Keyed {Guid.NewGuid():N}", key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quizId = await ReadIdAndKeyAsync(response, key);

        var get = await SendAdminRequestAsync(HttpMethod.Get, $"/api/admin/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        await ReadIdAndKeyAsync(get, key);
        Assert.Equal(key, await StoredKeyAsync(quizId));
    }

    [Fact]
    public async Task Create_without_a_content_key_leaves_it_empty_and_several_such_quizzes_coexist()
    {
        var first = await CreateAsync($"No key {Guid.NewGuid():N}", null);
        var second = await CreateAsync($"No key {Guid.NewGuid():N}", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Null(await StoredKeyAsync(await ReadIdAndKeyAsync(first, null)));
    }

    [Fact]
    public async Task Create_with_an_existing_content_key_returns_conflict_and_creates_nothing()
    {
        var key = NewKey();
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync($"First {Guid.NewGuid():N}", key)).StatusCode);
        var title = $"Second {Guid.NewGuid():N}";

        var response = await CreateAsync(title, key);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(DuplicateKey, await ReadErrorMessageAsync(response));
        await using var context = CreateContext();
        Assert.False(await context.Quizzs.AnyAsync(quiz => quiz.Title == title, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Concurrent_creates_with_the_same_content_key_yield_one_created_and_one_conflict()
    {
        var key = NewKey();

        var responses = await Task.WhenAll(
            CreateAsync($"Race A {Guid.NewGuid():N}", key),
            CreateAsync($"Race B {Guid.NewGuid():N}", key));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(DuplicateKey, await ReadErrorMessageAsync(conflict));
    }

    [Fact]
    public async Task Update_without_a_content_key_keeps_the_stored_one()
    {
        var key = NewKey();
        var quizId = await ReadIdAndKeyAsync(await CreateAsync($"Keep {Guid.NewGuid():N}", key), key);

        var response = await UpdateAsync(quizId, $"Renamed {Guid.NewGuid():N}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await ReadIdAndKeyAsync(response, key);
        Assert.Equal(key, await StoredKeyAsync(quizId));
    }

    [Fact]
    public async Task Update_with_the_same_content_key_succeeds()
    {
        var key = NewKey();
        var quizId = await ReadIdAndKeyAsync(await CreateAsync($"Same {Guid.NewGuid():N}", key), key);

        var response = await UpdateAsync(quizId, $"Renamed {Guid.NewGuid():N}", key);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(key, await StoredKeyAsync(quizId));
    }

    [Fact]
    public async Task Update_with_a_different_content_key_returns_bad_request_and_changes_nothing()
    {
        var key = NewKey();
        var title = $"Locked {Guid.NewGuid():N}";
        var quizId = await ReadIdAndKeyAsync(await CreateAsync(title, key), key);

        var response = await UpdateAsync(quizId, "Should not apply", NewKey());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ImmutableKey, await ReadErrorMessageAsync(response));
        Assert.Equal(key, await StoredKeyAsync(quizId));
        await using var context = CreateContext();
        Assert.Equal(title, await context.Quizzs.Where(quiz => quiz.Id == quizId).Select(quiz => quiz.Title).SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_with_a_content_key_on_a_quiz_without_one_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await UpdateAsync(quizId, "Renamed", NewKey());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ImmutableKey, await ReadErrorMessageAsync(response));
        Assert.Null(await StoredKeyAsync(quizId));
    }

    [Fact]
    public async Task Update_with_an_invalid_content_key_returns_bad_request()
    {
        var quizId = await CreateQuizAsync();

        var response = await UpdateAsync(quizId, "Renamed", "Not A Slug");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(InvalidKey, await ReadErrorMessageAsync(response));
    }

    private static string NewKey() => $"chave-{Guid.NewGuid():N}";

    private Task<HttpResponseMessage> CreateAsync(string title, string? contentKey) => SendAdminRequestAsync(
        HttpMethod.Post,
        "/api/admin/quizzes",
        new { title, difficulty = (int)Difficulty.EASY, category = (int)QuizCategory.GEOGRAPHY, contentKey });

    private Task<HttpResponseMessage> UpdateAsync(int quizId, string title, string? contentKey) => SendAdminRequestAsync(
        HttpMethod.Put,
        $"/api/admin/quizzes/{quizId}",
        new { title, difficulty = (int)Difficulty.EASY, category = (int)QuizCategory.GEOGRAPHY, contentKey });

    private async Task<string?> StoredKeyAsync(int quizId)
    {
        await using var context = CreateContext();
        return await context.Quizzs
            .Where(quiz => quiz.Id == quizId)
            .Select(quiz => quiz.ContentKey)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> ReadIdAndKeyAsync(HttpResponseMessage response, string? expectedKey)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var element = document.RootElement.GetProperty("contentKey");
        Assert.Equal(expectedKey, element.ValueKind == JsonValueKind.Null ? null : element.GetString());
        return document.RootElement.GetProperty("id").GetInt32();
    }
}
