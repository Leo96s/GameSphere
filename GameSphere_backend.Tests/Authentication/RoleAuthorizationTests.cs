using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using GameSphere_backend.Enums;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class RoleAuthorizationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly GameSphereApiFactory _factory;
    private WebApplicationFactory<Program> _application = null!;
    private HttpClient _client = null!;

    public RoleAuthorizationTests(PostgreSqlFixture database)
    {
        _factory = new GameSphereApiFactory(database);
    }

    public ValueTask InitializeAsync()
    {
        _application = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(AdminOnlyTestController).Assembly);
            });
        });
        _client = _application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _application.DisposeAsync();
    }

    [Fact]
    public void GenerateToken_includes_the_persisted_role_claim()
    {
        var token = GenerateToken(UserRole.Admin);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;

        Assert.Contains(claims, claim =>
            claim.Type == ClaimTypes.Role && claim.Value == UserRole.Admin.ToString());
    }

    [Fact]
    public async Task Admin_endpoint_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/_test/authorization", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoint_with_user_token_returns_forbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/_test/authorization");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.User));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoint_with_admin_token_returns_ok()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/_test/authorization");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string GenerateToken(UserRole role)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:SecretKey"] = "test-secret-with-at-least-thirty-two-bytes",
                ["JwtSettings:Issuer"] = "gamesphere-tests",
                ["JwtSettings:Audience"] = "gamesphere-tests",
                ["JwtSettings:ExpirationMinutes"] = "60"
            })
            .Build();

        return new AuthService(configuration).GenerateToken("42", "user@example.test", role);
    }
}

[ApiController]
[Route("_test/authorization")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminOnlyTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
