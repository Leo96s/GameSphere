#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using GameSphere_backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class AccountAnonymizerTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public AccountAnonymizerTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task AnonymizeExpiredAccountsAsync_anonymizes_an_account_past_the_30_day_window()
    {
        await using var context = CreateContext();
        var utcNow = DateTime.UtcNow;
        var user = CreateDeactivatedUser("expired@example.test", utcNow.AddDays(-31));
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var anonymizer = new AccountAnonymizer(context);
        var count = await anonymizer.AnonymizeExpiredAccountsAsync(utcNow, CancellationToken.None);

        Assert.Equal(1, count);
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.True(persistedUser.IsAnonymized);
        Assert.Equal($"deleted-user-{user.Id}@anonymized.gamesphere.invalid", persistedUser.Email);
        Assert.Equal("Deleted", persistedUser.FirstName);
        Assert.Equal(string.Empty, persistedUser.LastName);
        Assert.Null(persistedUser.Image);
        Assert.Null(persistedUser.UID);
        Assert.False(persistedUser.HasLocalPassword);
        Assert.Null(persistedUser.RecoveryCodeHash);
    }

    [Fact]
    public async Task AnonymizeExpiredAccountsAsync_does_not_touch_an_account_still_within_the_window()
    {
        await using var context = CreateContext();
        var utcNow = DateTime.UtcNow;
        var user = CreateDeactivatedUser("within-window@example.test", utcNow.AddDays(-10));
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var anonymizer = new AccountAnonymizer(context);
        var count = await anonymizer.AnonymizeExpiredAccountsAsync(utcNow, CancellationToken.None);

        Assert.Equal(0, count);
        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.False(persistedUser.IsAnonymized);
        Assert.Equal("within-window@example.test", persistedUser.Email);
    }

    [Fact]
    public async Task AnonymizeExpiredAccountsAsync_is_idempotent_for_an_already_anonymized_account()
    {
        await using var context = CreateContext();
        var utcNow = DateTime.UtcNow;
        var user = CreateDeactivatedUser("already-anonymized@example.test", utcNow.AddDays(-45));
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var anonymizer = new AccountAnonymizer(context);
        var firstRunCount = await anonymizer.AnonymizeExpiredAccountsAsync(utcNow, CancellationToken.None);
        Assert.Equal(1, firstRunCount);

        var anonymizedEmail = (await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken)).Email;

        var secondRunCount = await anonymizer.AnonymizeExpiredAccountsAsync(utcNow, CancellationToken.None);
        Assert.Equal(0, secondRunCount);

        var persistedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(anonymizedEmail, persistedUser.Email);
    }

    [Fact]
    public async Task AnonymizeExpiredAccountsAsync_preserves_existing_scores_and_quizzes()
    {
        await using var context = CreateContext();
        var utcNow = DateTime.UtcNow;
        var user = CreateDeactivatedUser("with-history@example.test", utcNow.AddDays(-31));
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var quizz = new Quizz
        {
            Title = "History quiz",
            RegistrationDate = utcNow,
            UserId = user.Id,
        };
        context.Quizzs.Add(quizz);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var score = new Score
        {
            UserId = user.Id,
            QuizzId = quizz.Id,
            Points = 42,
            Date = utcNow,
        };
        context.Scores.Add(score);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var anonymizer = new AccountAnonymizer(context);
        await anonymizer.AnonymizeExpiredAccountsAsync(utcNow, CancellationToken.None);

        var persistedScore = await context.Scores.SingleAsync(candidate => candidate.Id == score.Id, TestContext.Current.CancellationToken);
        var persistedQuizz = await context.Quizzs.SingleAsync(candidate => candidate.Id == quizz.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, persistedScore.UserId);
        Assert.Equal(42, persistedScore.Points);
        Assert.Equal(user.Id, persistedQuizz.UserId);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static User CreateDeactivatedUser(string email, DateTime deactivatedAt) => new()
    {
        Email = email,
        FirstName = "History",
        LastName = "User",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword("Current-password-123", 4),
        HasLocalPassword = true,
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = false,
        DeactivatedAt = deactivatedAt,
        RecoveryCodeHash = "stale-hash",
        Role = UserRole.User,
    };
}
