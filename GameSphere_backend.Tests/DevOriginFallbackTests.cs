using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests;

// Development fallback: no Cors:AllowedOrigins configured, so the built-in list applies
// to both the CORS policy and OriginProtectionMiddleware.
public sealed class DevOriginFallbackTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;

    public DevOriginFallbackTests(PostgreSqlFixture database)
    {
        _factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new System.Uri("https://localhost"),
        });
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/User/login");
        request.Headers.TryAddWithoutValidation("Origin", origin);
        request.Content = JsonContent.Create(new { email = "unknown@example.test", password = "irrelevant-password" });
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("https://localhost:5173")]
    [InlineData("https://127.0.0.1:5173")]
    [InlineData("http://localhost:5173")]
    [InlineData("http://127.0.0.1:5173")]
    public async Task Unsafe_requests_from_a_development_origin_are_not_blocked_by_origin_protection(string origin)
    {
        var response = await PostLoginAsync(origin);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("https://attacker.example")]
    [InlineData("https://localhost:5174")]
    [InlineData("https://localhost.attacker.example")]
    public async Task Unsafe_requests_from_other_origins_are_rejected(string origin)
    {
        var response = await PostLoginAsync(origin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cors_preflight_allows_the_https_development_origin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/User/login");
        request.Headers.TryAddWithoutValidation("Origin", "https://localhost:5173");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("https://localhost:5173", Assert.Single(values));
    }

    [Fact]
    public async Task Cors_preflight_does_not_allow_an_untrusted_origin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/User/login");
        request.Headers.TryAddWithoutValidation("Origin", "https://attacker.example");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
