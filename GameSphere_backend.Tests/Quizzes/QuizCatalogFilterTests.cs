#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameSphere_backend.Tests.Quizzes;

public sealed class QuizCatalogFilterTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;
    private int _ownerId;
    private string _suffix = null!;
    private int _geoEasy;
    private int _geoHard;
    private int _historyEasy;
    private int _geoDraft;

    public QuizCatalogFilterTests(PostgreSqlFixture database)
    {
        _database = database;
        _factory = new GameSphereApiFactory(database);
    }

    public async ValueTask InitializeAsync()
    {
        await SeedAsync();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Without_filters_every_published_quiz_is_returned()
    {
        var ids = await ListIdsAsync("/api/quizzes?pageSize=100");

        Assert.Contains(_geoEasy, ids);
        Assert.Contains(_geoHard, ids);
        Assert.Contains(_historyEasy, ids);
        Assert.DoesNotContain(_geoDraft, ids);
        Assert.Equal(ids.OrderBy(id => id).ToList(), ids);
    }

    [Fact]
    public async Task Filtering_by_category_returns_only_that_category()
    {
        var ids = await ListIdsAsync($"/api/quizzes?pageSize=100&category={(int)QuizCategory.GEOGRAPHY}");

        Assert.Equal([_geoEasy, _geoHard], ids.Where(Mine).ToList());
        Assert.DoesNotContain(_geoDraft, ids);
    }

    [Fact]
    public async Task Filtering_by_difficulty_returns_only_that_difficulty()
    {
        var ids = await ListIdsAsync($"/api/quizzes?pageSize=100&difficulty={(int)Difficulty.EASY}");

        Assert.Equal([_geoEasy, _historyEasy], ids.Where(Mine).ToList());
    }

    [Fact]
    public async Task Filters_combine_with_and()
    {
        var ids = await ListIdsAsync(
            $"/api/quizzes?pageSize=100&category={(int)QuizCategory.GEOGRAPHY}&difficulty={(int)Difficulty.HARD}");

        Assert.Equal([_geoHard], ids.Where(Mine).ToList());
    }

    [Fact]
    public async Task Filter_with_no_match_returns_an_empty_list()
    {
        var ids = await ListIdsAsync(
            $"/api/quizzes?pageSize=100&category={(int)QuizCategory.SPORTS}&difficulty={(int)Difficulty.MEDIUM}");

        Assert.Empty(ids.Where(Mine));
    }

    [Theory]
    [InlineData("category=99")]
    [InlineData("category=-1")]
    [InlineData("difficulty=99")]
    [InlineData("difficulty=3")]
    [InlineData("category=1&difficulty=99")]
    public async Task Undefined_filter_values_return_bad_request(string query)
    {
        using var request = CreateAuthorizedRequest($"/api/quizzes?{query}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid catalog filter.", JsonDocument.Parse(body).RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Filters_never_reveal_unpublished_or_archived_quizzes()
    {
        await using (var context = CreateContext())
        {
            context.Quizzs.AddRange(
                Quiz("Geo unpublished", QuizCategory.GEOGRAPHY, Difficulty.EASY, QuizStatus.Unpublished),
                Quiz("Geo archived", QuizCategory.GEOGRAPHY, Difficulty.EASY, QuizStatus.Archived));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var ids = await ListIdsAsync(
            $"/api/quizzes?pageSize=100&category={(int)QuizCategory.GEOGRAPHY}&difficulty={(int)Difficulty.EASY}");

        await using var check = CreateContext();
        var statuses = await check.Quizzs
            .Where(quiz => ids.Contains(quiz.Id))
            .Select(quiz => quiz.Status)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(statuses);
        Assert.All(statuses, status => Assert.Equal(QuizStatus.Published, status));
    }

    [Fact]
    public async Task Filtered_items_all_match_the_filters_and_carry_the_category()
    {
        using var request = CreateAuthorizedRequest(
            $"/api/quizzes?pageSize=100&category={(int)QuizCategory.HISTORY}&difficulty={(int)Difficulty.EASY}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = JsonDocument.Parse(body).RootElement.EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("id").GetInt32() == _historyEasy);
        Assert.All(items, item =>
        {
            Assert.Equal((int)QuizCategory.HISTORY, item.GetProperty("category").GetInt32());
            Assert.Equal((int)Difficulty.EASY, item.GetProperty("difficulty").GetInt32());
        });
    }

    [Fact]
    public async Task Pagination_clamp_still_applies_with_filters()
    {
        var ids = await ListIdsAsync($"/api/quizzes?page=0&pageSize=0&category={(int)QuizCategory.GEOGRAPHY}");

        Assert.Single(ids);
        Assert.Equal(ids.OrderBy(id => id).ToList(), ids);
    }

    [Theory]
    [InlineData("category=abc")]
    [InlineData("difficulty=abc")]
    [InlineData("difficulty=1.5")]
    [InlineData("category=1.5")]
    public async Task Non_numeric_filter_values_return_bad_request(string query)
    {
        using var request = CreateAuthorizedRequest($"/api/quizzes?{query}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Invalid catalog filter.", JsonDocument.Parse(body).RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Empty_filter_values_behave_like_no_filter()
    {
        var unfiltered = await ListIdsAsync("/api/quizzes?pageSize=100");
        var empty = await ListIdsAsync("/api/quizzes?pageSize=100&category=&difficulty=");

        Assert.Equal(unfiltered, empty);
    }

    [Fact]
    public async Task Filtering_still_requires_authentication()
    {
        var response = await _client.GetAsync("/api/quizzes?category=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private bool Mine(int id) => id == _geoEasy || id == _geoHard || id == _historyEasy || id == _geoDraft;

    private async Task<List<int>> ListIdsAsync(string uri)
    {
        using var request = CreateAuthorizedRequest(uri);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(body).RootElement.EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToList();
    }

    private async Task SeedAsync()
    {
        await using var context = CreateContext();
        _suffix = Guid.NewGuid().ToString("N");
        var owner = new User
        {
            Email = $"filter-owner-{_suffix}@example.test",
            FirstName = "Filter",
            LastName = "Owner",
            HashedPassword = "Test-password-123",
            RegistrationDate = DateTime.UtcNow,
            Gender = Gender.OUTRO,
            isActive = true,
        };
        context.Users.Add(owner);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ownerId = owner.Id;

        var geoEasy = Quiz("Geo easy", QuizCategory.GEOGRAPHY, Difficulty.EASY, QuizStatus.Published);
        var geoHard = Quiz("Geo hard", QuizCategory.GEOGRAPHY, Difficulty.HARD, QuizStatus.Published);
        var historyEasy = Quiz("History easy", QuizCategory.HISTORY, Difficulty.EASY, QuizStatus.Published);
        var geoDraft = Quiz("Geo draft", QuizCategory.GEOGRAPHY, Difficulty.EASY, QuizStatus.Draft);
        context.Quizzs.AddRange(geoEasy, geoHard, historyEasy, geoDraft);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _geoEasy = geoEasy.Id;
        _geoHard = geoHard.Id;
        _historyEasy = historyEasy.Id;
        _geoDraft = geoDraft.Id;
    }

    private Quizz Quiz(string title, QuizCategory category, Difficulty difficulty, QuizStatus status) => new()
    {
        Title = $"{title} {_suffix}",
        Category = category,
        Difficulty = difficulty,
        NumberOfQuests = 0,
        RegistrationDate = DateTime.UtcNow,
        UserId = _ownerId,
        Status = status,
        Questions = []
    };

    private AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_database.ConnectionString)
        .Options);

    private HttpRequestMessage CreateAuthorizedRequest(string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(_ownerId));
        return request;
    }

    private static string GenerateToken(int userId)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-with-at-least-thirty-two-bytes",
                ["JwtSettings:Issuer"] = "gamesphere-tests",
                ["JwtSettings:Audience"] = "gamesphere-tests",
                ["JwtSettings:ExpirationMinutes"] = "60"
            })
            .Build();

        return new AuthService(configuration).GenerateToken(
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "player@example.test",
            UserRole.User);
    }
}
