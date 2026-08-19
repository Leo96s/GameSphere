using System;
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
}
