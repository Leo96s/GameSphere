using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Xunit;

namespace GameSphere_backend.Tests;

public sealed class SwaggerIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;

    public SwaggerIntegrationTests(PostgreSqlFixture database)
    {
        _factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        _client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Swagger_is_available_in_development()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Development_cors_allows_the_local_frontend_origin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/quizzes");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Development_cors_does_not_allow_an_unknown_origin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/quizzes");
        request.Headers.Add("Origin", "https://attacker.example.test");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
