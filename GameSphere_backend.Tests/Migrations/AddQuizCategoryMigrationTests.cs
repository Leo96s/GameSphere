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
/// Uses its own database so it can stop at the migration before the category column, seed legacy rows and migrate up.
/// </summary>
public sealed class AddQuizCategoryMigrationTests : IAsyncLifetime
{
    private const string PreviousMigration = "20261010120000_ReplaceQuizPublicationWithStatus";
    private const string CategoryMigration = "20261025120000_AddQuizCategory";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    public static TheoryData<string, int, string> PrefixCases => new()
    {
        { "Cultura geral — Misto", 0, "Misto" },
        { "Geografia — Capitais da Europa", 1, "Capitais da Europa" },
        { "História — Roma antiga", 2, "Roma antiga" },
        { "Ciência — Sistema solar", 3, "Sistema solar" },
        { "Desporto — Futebol", 4, "Futebol" },
        { "Entretenimento — Cinema", 5, "Cinema" },
        { "Tecnologia — Redes", 6, "Redes" },
        { "Videojogos — Clássicos", 7, "Clássicos" },
        { "Geografia —  Europa", 1, "Europa" },
        { "Geografia —   Europa  ", 1, "Europa" },
    };

    [Theory]
    [MemberData(nameof(PrefixCases))]
    public async Task Up_derives_category_from_the_title_prefix_and_strips_it(string title, int expectedCategory, string expectedTitle)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);
        Assert.False(await ColumnExistsAsync("Category"));

        var ownerId = await InsertLegacyUserAsync();
        var id = await InsertQuizAsync(ownerId, title);

        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        Assert.True(await ColumnExistsAsync("Category"));
        Assert.Equal(expectedCategory, await ReadIntAsync($"SELECT \"Category\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
        Assert.Equal(expectedTitle, await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
        Assert.Equal(0, await ReadIntAsync(
            "SELECT COUNT(*)::int FROM information_schema.columns WHERE table_name = 'Quizzs' AND column_name = 'Category' AND is_nullable = 'YES'"));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellationToken));
    }

    [Theory]
    [InlineData("Capitais da Europa")]
    [InlineData("Geografia - hífen simples")]
    [InlineData("Geografia —")]
    [InlineData("Geografia — ")]
    [InlineData("Geografia —  ")]
    [InlineData("Geografia —     ")]
    [InlineData("Astronomia — Planetas")]
    public async Task Up_leaves_titles_without_a_recognised_prefix_in_general_knowledge(string title)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        var ownerId = await InsertLegacyUserAsync();
        var id = await InsertQuizAsync(ownerId, title);

        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        Assert.Equal(0, await ReadIntAsync($"SELECT \"Category\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
        Assert.Equal(title, await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
    }

    [Theory]
    [InlineData("Quiz de Geografia — Europa")]
    [InlineData("geografia — Europa")]
    [InlineData("Geografia—Europa")]
    [InlineData("Geografia – Europa")]
    [InlineData("Geografia —Europa")]
    public async Task Up_only_matches_the_exact_label_at_the_start_of_the_title(string title)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);
        var id = await InsertQuizAsync(await InsertLegacyUserAsync(), title);

        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        Assert.Equal(0, await ReadIntAsync($"SELECT \"Category\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
        Assert.Equal(title, await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
    }

    [Fact]
    public async Task Up_strips_only_the_first_prefix_and_handles_a_mixed_batch_in_one_run()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);
        var ownerId = await InsertLegacyUserAsync();
        var nested = await InsertQuizAsync(ownerId, "Geografia — Rios — Europa");
        var plain = await InsertQuizAsync(ownerId, "Sem prefixo");
        var empty = await InsertQuizAsync(ownerId, "Ciência —");
        var science = await InsertQuizAsync(ownerId, "Ciência — Química");

        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        Assert.Equal((1, "Rios — Europa"), await ReadRowAsync(nested));
        Assert.Equal((0, "Sem prefixo"), await ReadRowAsync(plain));
        Assert.Equal((0, "Ciência —"), await ReadRowAsync(empty));
        Assert.Equal((3, "Química"), await ReadRowAsync(science));
    }

    [Theory]
    [InlineData(88, true)]
    [InlineData(89, false)]
    public async Task Down_restores_the_prefix_only_when_the_result_fits_in_100_characters(int titleLength, bool restored)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);
        var title = new string('x', titleLength);
        var id = await InsertCategorisedQuizAsync(await InsertLegacyUserAsync(), title, 1);

        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        var expected = restored ? "Geografia — " + title : title;
        Assert.Equal(expected, await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));
    }

    [Fact]
    public async Task Down_restores_every_non_zero_label()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);
        var ownerId = await InsertLegacyUserAsync();
        var ids = new List<int>();
        for (var category = 0; category <= 7; category++)
        {
            ids.Add(await InsertCategorisedQuizAsync(ownerId, "T", category));
        }

        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        string[] expected = ["T", "Geografia — T", "História — T", "Ciência — T", "Desporto — T", "Entretenimento — T", "Tecnologia — T", "Videojogos — T"];
        for (var category = 0; category <= 7; category++)
        {
            Assert.Equal(expected[category], await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {ids[category]}"));
        }
    }

    private async Task<(int Category, string Title)> ReadRowAsync(int id) =>
        (await ReadIntAsync($"SELECT \"Category\" FROM \"Quizzs\" WHERE \"Id\" = {id}"),
         await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {id}"));

    [Fact]
    public async Task Down_restores_the_prefix_for_categorised_quizzes_and_drops_the_column()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        var ownerId = await InsertLegacyUserAsync();
        var geography = await InsertCategorisedQuizAsync(ownerId, "Capitais da Europa", 1);
        var videoGames = await InsertCategorisedQuizAsync(ownerId, "Clássicos", 7);
        var general = await InsertCategorisedQuizAsync(ownerId, "Sem categoria", 0);
        var longTitle = new string('x', 95);
        var tooLong = await InsertCategorisedQuizAsync(ownerId, longTitle, 1);

        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        Assert.False(await ColumnExistsAsync("Category"));
        Assert.Equal("Geografia — Capitais da Europa", await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {geography}"));
        Assert.Equal("Videojogos — Clássicos", await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {videoGames}"));
        Assert.Equal("Sem categoria", await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {general}"));
        Assert.Equal(longTitle, await ReadStringAsync($"SELECT \"Title\" FROM \"Quizzs\" WHERE \"Id\" = {tooLong}"));
    }

    [Fact]
    public async Task Up_adds_a_nullable_content_key_column_with_a_filtered_unique_index()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);
        Assert.False(await ColumnExistsAsync("ContentKey"));
        Assert.Equal(0, await ReadIntAsync("SELECT COUNT(*)::int FROM pg_indexes WHERE indexname = 'IX_Quizzs_ContentKey'"));

        var ownerId = await InsertLegacyUserAsync();
        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);

        Assert.Equal(1, await ReadIntAsync(
            "SELECT COUNT(*)::int FROM information_schema.columns WHERE table_name = 'Quizzs' AND column_name = 'ContentKey' " +
            "AND is_nullable = 'YES' AND data_type = 'character varying' AND character_maximum_length = 100"));
        var definition = await ReadStringAsync("SELECT indexdef FROM pg_indexes WHERE indexname = 'IX_Quizzs_ContentKey'");
        Assert.Contains("UNIQUE", definition, StringComparison.Ordinal);
        Assert.Contains("\"ContentKey\" IS NOT NULL", definition, StringComparison.Ordinal);

        // Several quizzes without a key coexist; a repeated key is rejected.
        await InsertQuizWithKeyAsync(ownerId, "A", null);
        await InsertQuizWithKeyAsync(ownerId, "B", null);
        await InsertQuizWithKeyAsync(ownerId, "C", "chave-unica");
        await Assert.ThrowsAsync<PostgresException>(() => InsertQuizWithKeyAsync(ownerId, "D", "chave-unica"));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(cancellationToken));
    }

    [Fact]
    public async Task Down_drops_the_content_key_column_and_its_index()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(CategoryMigration, cancellationToken: cancellationToken);
        Assert.True(await ColumnExistsAsync("ContentKey"));

        await migrator.MigrateAsync(PreviousMigration, cancellationToken: cancellationToken);

        Assert.False(await ColumnExistsAsync("ContentKey"));
        Assert.Equal(0, await ReadIntAsync("SELECT COUNT(*)::int FROM pg_indexes WHERE indexname = 'IX_Quizzs_ContentKey'"));
    }

    private Task<int> InsertQuizWithKeyAsync(int ownerId, string title, string? contentKey) => ReadIntAsync(
        $"INSERT INTO \"Quizzs\" (\"Title\", \"Difficulty\", \"NumberOfQuests\", \"Status\", \"Category\", \"ContentKey\", \"RegistrationDate\", \"UserId\") " +
        $"VALUES ('{title}', 0, 0, 0, 0, {(contentKey is null ? "NULL" : $"'{contentKey}'")}, now(), {ownerId}) RETURNING \"Id\"");

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

    private Task<int> InsertQuizAsync(int ownerId, string title) => ReadIntAsync(
        $"INSERT INTO \"Quizzs\" (\"Title\", \"Difficulty\", \"NumberOfQuests\", \"Status\", \"RegistrationDate\", \"UserId\") " +
        $"VALUES ('{title}', 0, 0, 0, now(), {ownerId}) RETURNING \"Id\"");

    private Task<int> InsertCategorisedQuizAsync(int ownerId, string title, int category) => ReadIntAsync(
        $"INSERT INTO \"Quizzs\" (\"Title\", \"Difficulty\", \"NumberOfQuests\", \"Status\", \"Category\", \"RegistrationDate\", \"UserId\") " +
        $"VALUES ('{title}', 0, 0, 0, {category}, now(), {ownerId}) RETURNING \"Id\"");

    private async Task<bool> ColumnExistsAsync(string column) => await ReadIntAsync(
        $"SELECT COUNT(*)::int FROM information_schema.columns WHERE table_name = 'Quizzs' AND column_name = '{column}'") == 1;

    private async Task<int> ReadIntAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<string> ReadStringAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
