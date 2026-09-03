using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace GameSphere_backend.Tests;

public sealed class HostFilteringIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly GameSphereApiFactory _factory;
    private HttpClient _client = null!;

    public HostFilteringIntegrationTests(PostgreSqlFixture database)
    {
        _factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("AllowedHosts", "allowed.example.test"));

        _client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
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

    [Fact]
    public async Task Request_with_an_unrecognized_host_header_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/quizzes");
        request.Headers.Host = "attacker.example.test";

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_with_the_configured_host_header_reaches_the_application()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/quizzes");
        request.Headers.Host = "allowed.example.test";

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
