#nullable enable

using System;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class PasswordRecoveryServiceTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public PasswordRecoveryServiceTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task Password_recovery_rejects_malformed_code_before_database_lookup()
    {
        var context = CreateContext();
        var service = CreateService(context);

        await context.DisposeAsync();

        var response = await service.ValidateResetCode("player@example.test", "12AB56");

        Assert.False(response.Success);
        Assert.False(response.Data);
        Assert.Equal("BadRequest", response.Type);
        Assert.Equal("Invalid or expired reset code!", response.Message);
    }

    [Theory]
    [InlineData("not-an-email", "123456")]
    [InlineData("player@example.test", "12AB56")]
    [InlineData("player@example.test", "12345")]
    [InlineData("player@example.test", "1234567")]
    public void Password_reset_input_validator_rejects_invalid_email_or_code(string email, string resetCode)
    {
        var validator = new PasswordResetInputValidator();

        var message = validator.ValidateRequest(email, resetCode);

        Assert.Equal("Invalid or expired reset code!", message);
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("This-password-is-intentionally-longer-than-one-hundred-and-twenty-eight-characters-for-boundary-testing-only-1234567890-extra-extra-extra")]
    public void Password_reset_input_validator_rejects_invalid_password_length(string password)
    {
        var validator = new PasswordResetInputValidator();

        var message = validator.ValidateRequest("player@example.test", "123456", password);

        Assert.Equal("Invalid password recovery data.", message);
    }

    [Fact]
    public async Task Password_recovery_rejects_expired_reset_code()
    {
        await using var context = CreateContext();
        const string resetCode = "135790";
        var user = CreateUser("expired-code@example.test");
        user.ResetCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(resetCode, 4);
        user.ResetCodeAttempts = 0;
        user.ResetCodeExpiration = DateTime.UtcNow.AddMinutes(-1);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(context);

        var response = await service.ValidateResetCode(user.Email, resetCode);

        Assert.False(response.Success);
        Assert.False(response.Data);
        Assert.Equal("BadRequest", response.Type);
        Assert.Equal("Invalid or expired reset code!", response.Message);
    }

    [Fact]
    public async Task Password_recovery_clears_the_code_on_the_fifth_invalid_attempt()
    {
        await using var context = CreateContext();
        const string resetCode = "246802";
        var user = CreateUser("fifth-attempt@example.test");
        user.ResetCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(resetCode, 4);
        user.ResetCodeAttempts = 4;
        user.ResetCodeExpiration = DateTime.UtcNow.AddMinutes(15);
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(context);

        var response = await service.ValidateResetCode(user.Email, "000000");

        Assert.False(response.Success);
        Assert.False(response.Data);
        Assert.Equal("BadRequest", response.Type);

        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Null(persistedUser.ResetCodeHash);
        Assert.Null(persistedUser.ResetCodeExpiration);
        Assert.Equal(5, persistedUser.ResetCodeAttempts);
    }

    private static PasswordRecoveryService CreateService(AppDbContext context)
    {
        var resetCodeService = new PasswordResetCodeService(context);
        return new PasswordRecoveryService(
            context,
            new TestEmailService(),
            new PasswordResetInputValidator(),
            resetCodeService);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser(string email) => new()
    {
        Email = email,
        FirstName = "Reset",
        LastName = "User",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword("Current-password-123", 4),
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true,
        Role = UserRole.User,
    };

    private sealed class TestEmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(string to, string subject, string body) => Task.FromResult(true);
    }
}
