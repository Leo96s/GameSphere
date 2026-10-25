#nullable enable

using System;
using GameSphere_backend.Services.QuizAdministration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class QuizAdministrationDataGuardsTests
{
    private static DbUpdateException Wrap(Exception inner) => new("save failed", inner);

    private static PostgresException Postgres(string sqlState, string? constraint) =>
        new("error", "ERROR", "ERROR", sqlState, constraintName: constraint);

    [Fact]
    public void Unique_violation_on_the_content_key_index_is_detected()
    {
        Assert.True(QuizAdministrationDataGuards.IsContentKeyUniqueViolation(
            Wrap(Postgres(PostgresErrorCodes.UniqueViolation, "IX_Quizzs_ContentKey"))));
    }

    [Fact]
    public void Unique_violation_on_another_constraint_is_not_detected()
    {
        Assert.False(QuizAdministrationDataGuards.IsContentKeyUniqueViolation(
            Wrap(Postgres(PostgresErrorCodes.UniqueViolation, "IX_Users_Email"))));
    }

    [Fact]
    public void Another_sql_state_on_the_content_key_index_is_not_detected()
    {
        Assert.False(QuizAdministrationDataGuards.IsContentKeyUniqueViolation(
            Wrap(Postgres(PostgresErrorCodes.ForeignKeyViolation, "IX_Quizzs_ContentKey"))));
    }

    [Fact]
    public void Non_postgres_inner_exception_is_not_detected()
    {
        Assert.False(QuizAdministrationDataGuards.IsContentKeyUniqueViolation(Wrap(new InvalidOperationException("x"))));
        Assert.False(QuizAdministrationDataGuards.IsContentKeyUniqueViolation(new DbUpdateException("save failed")));
    }
}
