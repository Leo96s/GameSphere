#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class InitialAdminBootstrapperTests : IClassFixture<PostgreSqlFixture>
{
    private const string AdminEmail = "admin@example.test";
    private const string AdminPassword = "Test-only-admin-password-123";
    private readonly PostgreSqlFixture _database;

    public InitialAdminBootstrapperTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task EnsureAdminAsync_creates_the_configured_active_admin()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var bootstrapper = CreateBootstrapper(context);

        await bootstrapper.EnsureAdminAsync(TestContext.Current.CancellationToken);

        var admin = await context.Users.SingleAsync(user => user.Email == AdminEmail, TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(admin.isActive);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify(AdminPassword, admin.HashedPassword));
    }

    [Fact]
    public async Task EnsureAdminAsync_promotes_an_existing_configured_user()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        context.Users.Add(CreateUser(UserRole.User));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var bootstrapper = CreateBootstrapper(context);

        await bootstrapper.EnsureAdminAsync(TestContext.Current.CancellationToken);

        var admin = await context.Users.SingleAsync(user => user.Email == AdminEmail, TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public async Task EnsureAdminAsync_is_idempotent_for_the_configured_email()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var bootstrapper = CreateBootstrapper(context);

        await bootstrapper.EnsureAdminAsync(TestContext.Current.CancellationToken);
        await bootstrapper.EnsureAdminAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, await context.Users.CountAsync(user => user.Email == AdminEmail, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureAdminAsync_creates_one_configured_admin_when_started_concurrently()
    {
        await using (var context = CreateContext())
        {
            await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await using var context = CreateContext();
            await CreateBootstrapper(context).EnsureAdminAsync(TestContext.Current.CancellationToken);
        })));

        await using var verificationContext = CreateContext();
        var admins = await verificationContext.Users
            .Where(user => user.Email == AdminEmail)
            .ToListAsync(TestContext.Current.CancellationToken);

        var admin = Assert.Single(admins);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public void UserToModel_assigns_user_role_when_client_submits_admin_role()
    {
        var request = DeserializeUserRequest(UserRole.Admin);

        Assert.Equal(UserRole.User, request.Role);

        var user = UserMapper.UserToModel(request);

        Assert.NotNull(user);
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public async Task EditUserAsync_preserves_the_persisted_admin_role_when_client_submits_user_role()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var admin = CreateUser(UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = DeserializeUserRequest(UserRole.User);
        var service = CreateUserService(context);

        await service.EditUserAsync(admin.Id, request);

        var persistedUser = await context.Users.SingleAsync(user => user.Id == admin.Id, TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Admin, persistedUser.Role);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static InitialAdminBootstrapper CreateBootstrapper(AppDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InitialAdmin:Email"] = AdminEmail,
                ["InitialAdmin:Password"] = AdminPassword,
            })
            .Build();

        return new InitialAdminBootstrapper(context, configuration);
    }

    private static UserServices CreateUserService(AppDbContext context) =>
        new(context, new TestAuthService(), new TestEmailService());

    private static UserDto DeserializeUserRequest(UserRole role)
    {
        var json = $$"""
        {
          "firstName": "Client",
          "lastName": "Request",
          "email": "client@example.test",
          "uid": "1",
          "hashedPassword": "Client-password-123",
          "gender": 2,
          "isActive": true,
          "role": {{(int)role}}
        }
        """;

        return JsonSerializer.Deserialize<UserDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The user request must deserialize.");
    }

    private static User CreateUser(UserRole role) => new()
    {
        Email = AdminEmail,
        FirstName = "Existing",
        LastName = "User",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword("Existing-password-123", 13),
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true,
        Role = role,
    };

    private sealed class TestAuthService : IAuthService
    {
        public string GenerateToken(string userId, string email) => string.Empty;
    }

    private sealed class TestEmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(string to, string subject, string body) => Task.FromResult(true);
    }
}
