using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameSphere_backend.Services.QuizAdministration;

internal static class QuizAdministrationDataGuards
{
    public static async Task<bool> IsPersistedAdminAsync(this AppDbContext context, int adminId) =>
        await context.Users.AnyAsync(user => user.Id == adminId && user.Role == UserRole.Admin);

    public static async Task<Quizz?> GetQuizForUpdateAsync(this AppDbContext context, int quizId) =>
        await context.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {quizId} FOR UPDATE")
            .SingleOrDefaultAsync();

    /// <summary>
    /// Locks the quiz row in shared mode: concurrent attempts proceed in parallel,
    /// but wait for (and block) an administrator's FOR UPDATE lock on the same row.
    /// </summary>
    public static async Task<Quizz?> GetQuizForShareAsync(this AppDbContext context, int quizId) =>
        await context.Quizzs
            .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {quizId} FOR SHARE")
            .SingleOrDefaultAsync();

    public static async Task<bool> HasAttemptsAsync(this AppDbContext context, int quizId) =>
        await context.Scores.AnyAsync(score => score.QuizzId == quizId);

    public static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}
