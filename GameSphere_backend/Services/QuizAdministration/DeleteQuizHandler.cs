using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class DeleteQuizHandler
{
    private readonly AppDbContext _context;

    public DeleteQuizHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<bool>> HandleAsync(int adminId, int quizId)
    {
        if (!await _context.IsPersistedAdminAsync(adminId))
        {
            return QuizAdministrationResponses.Failure<bool>(
                "NotFound",
                "The authenticated administrator was not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = await _context.GetQuizForUpdateAsync(quizId);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<bool>("NotFound", "Quiz not found.");
            }

            if (await _context.Scores.AnyAsync(score => score.QuizzId == quizId))
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<bool>(
                    "Conflict",
                    "A quiz with existing attempts cannot be deleted.");
            }

            await _context.Entry(quiz).Collection(candidate => candidate.Questions!).LoadAsync();
            _context.Set<Question>().RemoveRange(quiz.Questions ?? []);
            _context.Quizzs.Remove(quiz);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(true, "NoContent");
        }
        catch (DbUpdateException exception) when (QuizAdministrationDataGuards.IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync();
            return QuizAdministrationResponses.Failure<bool>(
                "Conflict",
                "The quiz could not be deleted because of related data.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
