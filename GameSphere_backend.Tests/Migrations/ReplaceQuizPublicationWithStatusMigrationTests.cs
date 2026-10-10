#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameSphere_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameSphere_backend.Tests.Migrations;

/// <summary>
/// Uses its own database so it can stop at the migration before the status change, seed legacy rows and migrate up.
/// </summary>
public sealed class ReplaceQuizPublicationWithStatusMigrationTests : IAsyncLifetime
{
    private const string PreviousMigration = "20260903192727_AddAccountDeactivationRecovery";
    private const string StatusMigration = "20261010120000_ReplaceQuizPublicationWithStatus";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Up_backfills_status_from_is_published_and_drops_the_old_column()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);
        Assert.True(await ColumnExistsAsync("IsPublished"));
        Assert.False(await ColumnExistsAsync("Status"));

        var ownerId = await InsertLegacyUserAsync();
        var publishedId = await InsertLegacyQuizAsync(ownerId, "Legacy published", isPublished: true);
        var draftId = await InsertLegacyQuizAsync(ownerId, "Legacy draft", isPublished: false);

        await migrator.MigrateAsync(StatusMigration, cancellationToken: cancellationToken);

        Assert.False(await ColumnExistsAsync("IsPublished"));
        Assert.True(await ColumnExistsAsync("Status"));
        Assert.Equal(1, await ReadIntAsync($"SELECT \"Status\" FROM \"Quizzs\" WHERE \"Id\" = {publishedId}"));
        Assert.Equal(0, await ReadIntAsync($"SELECT \"Status\" FROM \"Quizzs\" WHERE \"Id\" = {draftId}"));
        Assert.Equal(0, await ReadIntAsync(
            "SELECT COUNT(*)::int FROM information_schema.columns WHERE table_name = 'Quizzs' AND column_name = 'Status' AND is_nullable = 'YES'"));

        Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellationToken));
    }

    [Fact]
    public async Task Down_restores_is_published_only_for_published_quizzes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(StatusMigration, cancellationToken: cancellationToken);

        var ownerId = await InsertLegacyUserAsync();
        var ids = new Dictionary<int, int>();
        foreach (var status in new[] { 0, 1, 2, 3 })
        {
            ids[status] = await ReadIntAsync(
                $"INSERT INTO \"Quizzs\" (\"Title\", \"Difficulty\", \"NumberOfQuests\", \"Status\", \"RegistrationDate\", \"UserId\") " +
                $"VALUES ('Status {status}', 0, 0, {status}, now(), {ownerId}) RETURNING \"Id\"");
        }

        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        Assert.False(await ColumnExistsAsync("Status"));
        Assert.Equal(0, await ReadIntAsync($"SELECT \"IsPublished\"::int FROM \"Quizzs\" WHERE \"Id\" = {ids[0]}"));
        Assert.Equal(1, await ReadIntAsync($"SELECT \"IsPublished\"::int FROM \"Quizzs\" WHERE \"Id\" = {ids[1]}"));
        Assert.Equal(0, await ReadIntAsync($"SELECT \"IsPublished\"::int FROM \"Quizzs\" WHERE \"Id\" = {ids[2]}"));
        Assert.Equal(0, await ReadIntAsync($"SELECT \"IsPublished\"::int FROM \"Quizzs\" WHERE \"Id\" = {ids[3]}"));
    }

    private AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_container.GetConnectionString())
        .Options);

    private async Task<int> InsertLegacyUserAsync()
    {
        // Fill every NOT NULL column without a default so the insert does not depend on the exact user schema.
        var columns = new List<(string Name, string Type)>();
        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT column_name, data_type FROM information_schema.columns " +
                "WHERE table_name = 'Users' AND is_nullable = 'NO' AND column_default IS NULL AND is_identity = 'NO'",
                connection);
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                columns.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        var suffix = Guid.NewGuid().ToString("N");
        var values = columns.Select(column => column.Type switch
        {
            "integer" or "bigint" or "smallint" or "real" or "double precision" or "numeric" => "0",
            "boolean" => "TRUE",
            "timestamp with time zone" or "timestamp without time zone" => "now()",
            _ => column.Name == "Email" ? $"'legacy-{suffix}@example.test'" : $"'legacy-{suffix}'"
        });

        return await ReadIntAsync(
            $"INSERT INTO \"Users\" ({string.Join(", ", columns.Select(column => $"\"{column.Name}\""))}) " +
            $"VALUES ({string.Join(", ", values)}) RETURNING \"Id\"");
    }

    private Task<int> InsertLegacyQuizAsync(int ownerId, string title, bool isPublished) => ReadIntAsync(
        $"INSERT INTO \"Quizzs\" (\"Title\", \"Difficulty\", \"NumberOfQuests\", \"IsPublished\", \"RegistrationDate\", \"UserId\") " +
        $"VALUES ('{title}', 0, 0, {(isPublished ? "TRUE" : "FALSE")}, now(), {ownerId}) RETURNING \"Id\"");

    private async Task<bool> ColumnExistsAsync(string column) => await ReadIntAsync(
        $"SELECT COUNT(*)::int FROM information_schema.columns WHERE table_name = 'Quizzs' AND column_name = '{column}'") == 1;

    private async Task<int> ReadIntAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }
}
