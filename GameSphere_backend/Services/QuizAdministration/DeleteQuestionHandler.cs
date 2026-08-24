using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class DeleteQuestionHandler
{
    private readonly AppDbContext _context;

    public DeleteQuestionHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<bool>> HandleAsync(int adminId, int questionId)
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
            var quizId = await GetQuestionQuizIdAsync(questionId);
            if (quizId is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<bool>("NotFound", "Question not found.");
            }

            var quiz = await _context.GetQuizForUpdateAsync(quizId.Value);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<bool>("NotFound", "Quiz not found.");
            }

            var question = await _context.Set<Question>().SingleOrDefaultAsync(candidate => candidate.Id == questionId);
            if (question is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<bool>("NotFound", "Question not found.");
            }

            _context.Set<Question>().Remove(question);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == quiz.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(true, "NoContent");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<int?> GetQuestionQuizIdAsync(int questionId) =>
        await _context.Set<Question>()
            .Where(candidate => candidate.Id == questionId)
            .Select(candidate => (int?)candidate.QuizzId)
            .SingleOrDefaultAsync();
}
