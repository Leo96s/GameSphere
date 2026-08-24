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

    public static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}
