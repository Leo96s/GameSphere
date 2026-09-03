#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class AccountDeactivationRecoveryTests : UserAccountTestBase, IClassFixture<PostgreSqlFixture>
{
    public AccountDeactivationRecoveryTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task DeactivateAccount_with_correct_credentials_blocks_login_and_protected_access()
    {
        var (owner, _) = await SeedUsersAsync();

        using var deactivateRequest = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/deactivate", owner);
        deactivateRequest.Content = JsonContent.Create(new { currentPassword = "Test-password-123" });
        var deactivateResponse = await Client.SendAsync(deactivateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/User/login",
            new { email = owner.Email, password = "Test-password-123" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);

        using var protectedRequest = CreateAuthorizedRequest(HttpMethod.Get, $"/api/User/by-id/{owner.Id}", owner);
        var protectedResponse = await Client.SendAsync(protectedRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, protectedResponse.StatusCode);

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.False(persistedUser.isActive);
        Assert.NotNull(persistedUser.DeactivatedAt);
    }

    [Fact]
    public async Task DeactivateAccount_with_wrong_credentials_is_rejected()
    {
        var (owner, _) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/deactivate", owner);
        request.Content = JsonContent.Create(new { currentPassword = "not-the-password" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.True(persistedUser.isActive);
    }

    [Fact]
    public async Task DeactivateAccount_cannot_target_another_users_account()
    {
        var (owner, other) = await SeedUsersAsync();

        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{other.Id}/deactivate", owner);
        request.Content = JsonContent.Create(new { currentPassword = "Test-password-123" });
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == other.Id, TestContext.Current.CancellationToken);
        Assert.True(persistedUser.isActive);
    }

    [Fact]
    public async Task RecoverAccount_with_valid_code_within_window_reactivates_the_account()
    {
        var (owner, _) = await SeedUsersAsync();
        var recoveryCode = await DeactivateAndCaptureRecoveryCodeAsync(owner);

        var recoverResponse = await Client.PostAsJsonAsync(
            "/api/User/recover-account",
            new { email = owner.Email, code = recoveryCode },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, recoverResponse.StatusCode);

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.True(persistedUser.isActive);
        Assert.Null(persistedUser.DeactivatedAt);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/User/login",
            new { email = owner.Email, password = "Test-password-123" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task RecoverAccount_with_wrong_code_does_not_reactivate()
    {
        var (owner, _) = await SeedUsersAsync();
        await DeactivateAndCaptureRecoveryCodeAsync(owner);

        var recoverResponse = await Client.PostAsJsonAsync(
            "/api/User/recover-account",
            new { email = owner.Email, code = "000000" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, recoverResponse.StatusCode);

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == owner.Id, TestContext.Current.CancellationToken);
        Assert.False(persistedUser.isActive);
    }

    [Fact]
    public async Task RecoverAccount_for_an_account_that_was_never_deactivated_returns_generic_failure()
    {
        var (owner, _) = await SeedUsersAsync();

        var recoverResponse = await Client.PostAsJsonAsync(
            "/api/User/recover-account",
            new { email = owner.Email, code = "123456" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, recoverResponse.StatusCode);
    }

    [Fact]
    public async Task RequestRecoveryCode_for_an_unknown_email_returns_the_generic_success_message()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/User/request-account-recovery",
            $"unknown-{Guid.NewGuid():N}@example.test",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> DeactivateAndCaptureRecoveryCodeAsync(User owner)
    {
        using var deactivateRequest = CreateAuthorizedRequest(HttpMethod.Post, $"/api/User/{owner.Id}/deactivate", owner);
        deactivateRequest.Content = JsonContent.Create(new { currentPassword = "Test-password-123" });
        var deactivateResponse = await Client.SendAsync(deactivateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var codeMatch = Regex.Match(EmailService.LastBody, "\\b[0-9]{6}\\b");
        Assert.True(codeMatch.Success, "The fake email service should receive a six-digit recovery code.");
        return codeMatch.Value;
    }
}
