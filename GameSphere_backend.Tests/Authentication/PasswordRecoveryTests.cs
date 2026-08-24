#nullable enable

using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Authentication;

public sealed class PasswordRecoveryTests : UserAccountTestBase, IClassFixture<PostgreSqlFixture>
{
    public PasswordRecoveryTests(PostgreSqlFixture database)
        : base(database)
    {
    }

    [Fact]
    public async Task Password_recovery_accepts_the_email_code_and_new_password()
    {
        var (_, user) = await SeedUsersAsync();
        const string newPassword = "Recovered-password-123";

        var sendResponse = await Client.PostAsJsonAsync(
            "/api/User/send-reset-code",
            user.Email,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var resetCodeMatch = Regex.Match(EmailService.LastBody, "\\b[0-9]{6}\\b");
        Assert.True(resetCodeMatch.Success, "The fake email service should receive a six-digit reset code.");
        var resetCode = resetCodeMatch.Value;

        var validationResponse = await Client.PostAsJsonAsync(
            "/api/User/validate-reset-code",
            new { email = user.Email, resetCode },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);

        var resetResponse = await Client.PostAsJsonAsync(
            "/api/User/reset-password",
            new { email = user.Email, resetCode, newPassword },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/User/login",
            new { email = user.Email, password = newPassword },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Password_recovery_clears_the_code_after_five_invalid_attempts()
    {
        var (_, user) = await SeedUsersAsync();
        var sendResponse = await Client.PostAsJsonAsync(
            "/api/User/send-reset-code",
            user.Email,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var invalidResponse = await Client.PostAsJsonAsync(
                "/api/User/validate-reset-code",
                new { email = user.Email, resetCode = "000000" },
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        }

        await using var context = CreateContext();
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Null(persistedUser.ResetCodeHash);
        Assert.Null(persistedUser.ResetCodeExpiration);
        Assert.Equal(5, persistedUser.ResetCodeAttempts);
    }
}
