#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GameSphere_backend.Controllers;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using GameSphere_backend.ServicesResponses;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameSphere_backend.Tests.Admin;

public sealed class AdminAuthorizationTests : AdminQuizAdministrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string BootstrapAdminEmail = "admin@example.test";
    private const string BootstrapAdminPassword = "Test-only-admin-password-123";

    public AdminAuthorizationTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Administration_without_a_token_returns_unauthorized()
    {
        var response = await Client.GetAsync("/api/admin/quizzes", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administration_with_a_user_role_returns_forbidden()
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/admin/quizzes", UserRole.User, UserId);

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administration_with_an_inactive_admin_session_returns_forbidden_for_reads_and_writes()
    {
        using var readRequest = CreateRequest(HttpMethod.Get, "/api/admin/quizzes", UserRole.Admin, InactiveAdminId);
        using var writeRequest = CreateRequest(HttpMethod.Post, "/api/admin/quizzes", UserRole.Admin, InactiveAdminId, new
        {
            title = "Inactive admin quiz",
            difficulty = (int)Difficulty.EASY,
            isPublished = false
        });

        var readResponse = await Client.SendAsync(readRequest, TestContext.Current.CancellationToken);
        var writeResponse = await Client.SendAsync(writeRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }

    [Fact]
    public async Task Administration_controller_uses_sub_claim_when_name_identifier_is_absent()
    {
        var service = new CapturingQuizAdministrationService();
        var controller = new AdminQuizzesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(JwtRegisteredClaimNames.Sub, AdminId.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                        "test"))
                }
            }
        };

        var result = await controller.CreateQuiz(new AdminQuizUpsertDto
        {
            Title = "Claim fallback quiz",
            Difficulty = Difficulty.EASY,
            IsPublished = false
        });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(AdminId, service.CapturedAdminId);
    }

    [Fact]
    public async Task EnsureAdminAsync_creates_the_configured_active_admin()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var bootstrapper = CreateBootstrapper(context);

        await bootstrapper.EnsureAdminAsync(TestContext.Current.CancellationToken);

        var admin = await context.Users.SingleAsync(user => user.Email == BootstrapAdminEmail, TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(admin.isActive);
        Assert.True(BCrypt.Net.BCrypt.EnhancedVerify(BootstrapAdminPassword, admin.HashedPassword));
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

        var admin = await context.Users.SingleAsync(user => user.Email == BootstrapAdminEmail, TestContext.Current.CancellationToken);
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

        Assert.Equal(1, await context.Users.CountAsync(user => user.Email == BootstrapAdminEmail, TestContext.Current.CancellationToken));
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
            .Where(user => user.Email == BootstrapAdminEmail)
            .ToListAsync(TestContext.Current.CancellationToken);

        var admin = Assert.Single(admins);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public void InitialAdminConfiguration_normalizes_the_configured_email()
    {
        var configuration = CreateConfiguration("  ADMIN@EXAMPLE.TEST  ", BootstrapAdminPassword);

        var adminConfiguration = InitialAdminConfiguration.Load(configuration);

        Assert.Equal(BootstrapAdminEmail, adminConfiguration.Email);
        Assert.Equal(BootstrapAdminPassword, adminConfiguration.Password);
    }

    [Fact]
    public void InitialAdminConfiguration_rejects_invalid_email_without_echoing_the_value()
    {
        var configuration = CreateConfiguration("not-an-email", BootstrapAdminPassword);

        var exception = Assert.Throws<InvalidOperationException>(() => InitialAdminConfiguration.Load(configuration));

        Assert.Contains("InitialAdmin:Email", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not-an-email", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InitialAdminConfiguration_rejects_short_password_without_echoing_the_value()
    {
        var configuration = CreateConfiguration(BootstrapAdminEmail, "short");

        var exception = Assert.Throws<InvalidOperationException>(() => InitialAdminConfiguration.Load(configuration));

        Assert.Contains("InitialAdmin:Password", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("short", exception.Message, StringComparison.Ordinal);
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
    public async Task EditUserAsync_preserves_the_persisted_admin_role_when_updating_profile()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var admin = CreateUser(UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var request = CreateUpdateUserRequest();
        var service = CreateUserProfileService(context);

        await service.EditUserAsync(admin.Id, request);

        var persistedUser = await context.Users.SingleAsync(user => user.Id == admin.Id, TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Admin, persistedUser.Role);
    }

    private static InitialAdminBootstrapper CreateBootstrapper(AppDbContext context)
        => new(
            InitialAdminConfiguration.Load(CreateConfiguration(BootstrapAdminEmail, BootstrapAdminPassword)),
            new InitialAdminProvisioner(context));

    private static IConfiguration CreateConfiguration(string email, string password)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InitialAdmin:Email"] = email,
                ["InitialAdmin:Password"] = password,
            })
            .Build();
    }

    private static UserProfileService CreateUserProfileService(AppDbContext context) =>
        new(context);

    private static UpdateUserRequest CreateUpdateUserRequest() => new()
    {
        FirstName = "Client",
        LastName = "Request",
        Gender = Gender.OUTRO,
        Image = null,
    };

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
        Email = BootstrapAdminEmail,
        FirstName = "Existing",
        LastName = "User",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword("Existing-password-123", 13),
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true,
        Role = role,
    };

    private sealed class CapturingQuizAdministrationService : IQuizAdministrationService
    {
        public int? CapturedAdminId { get; private set; }

        public Task<IReadOnlyList<AdminQuizUpsertDto>> GetQuizzesAsync(int page = 1, int pageSize = 50)
            => Task.FromResult<IReadOnlyList<AdminQuizUpsertDto>>([]);

        public Task<AdminQuizUpsertDto?> GetQuizByIdAsync(int quizId)
            => Task.FromResult<AdminQuizUpsertDto?>(null);

        public Task<ServiceResponse<AdminQuizUpsertDto>> CreateQuizAsync(int adminId, AdminQuizUpsertDto request)
        {
            CapturedAdminId = adminId;
            return Task.FromResult(new ServiceResponse<AdminQuizUpsertDto>
            {
                Success = true,
                Data = new AdminQuizUpsertDto
                {
                    Id = 123,
                    Title = request.Title,
                    Difficulty = request.Difficulty,
                    IsPublished = request.IsPublished
                }
            });
        }

        public Task<ServiceResponse<AdminQuizUpsertDto>> UpdateQuizAsync(
            int adminId,
            int quizId,
            AdminQuizUpsertDto request)
            => Task.FromResult(new ServiceResponse<AdminQuizUpsertDto> { Success = false, Type = "NotFound" });

        public Task<ServiceResponse<bool>> DeleteQuizAsync(int adminId, int quizId)
            => Task.FromResult(new ServiceResponse<bool> { Success = false, Type = "NotFound" });

        public Task<ServiceResponse<AdminQuestionUpsertDto>> CreateQuestionAsync(
            int adminId,
            int quizId,
            AdminQuestionUpsertDto request)
            => Task.FromResult(new ServiceResponse<AdminQuestionUpsertDto> { Success = false, Type = "NotFound" });

        public Task<ServiceResponse<AdminQuestionUpsertDto>> UpdateQuestionAsync(
            int adminId,
            int questionId,
            AdminQuestionUpsertDto request)
            => Task.FromResult(new ServiceResponse<AdminQuestionUpsertDto> { Success = false, Type = "NotFound" });

        public Task<ServiceResponse<bool>> DeleteQuestionAsync(int adminId, int questionId)
            => Task.FromResult(new ServiceResponse<bool> { Success = false, Type = "NotFound" });
    }
}
