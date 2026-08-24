using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class UpdateQuizHandler
{
    private readonly AppDbContext _context;
    private readonly QuizRequestNormalizer _quizRequestNormalizer;

    public UpdateQuizHandler(AppDbContext context, QuizRequestNormalizer quizRequestNormalizer)
    {
        _context = context;
        _quizRequestNormalizer = quizRequestNormalizer;
    }

    public async Task<ServiceResponse<AdminQuizUpsertDto>> HandleAsync(
        int adminId,
        int quizId,
        AdminQuizUpsertDto request)
    {
        if (!_quizRequestNormalizer.TryNormalize(request, out var normalized, out var message))
        {
            return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>("BadRequest", message);
        }

        if (!await _context.IsPersistedAdminAsync(adminId))
        {
            return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>(
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
                return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>("NotFound", "Quiz not found.");
            }

            await _context.Entry(quiz).Collection(candidate => candidate.Questions!).LoadAsync();

            quiz.Title = normalized.Title;
            quiz.Difficulty = normalized.Difficulty;
            quiz.IsPublished = normalized.IsPublished;

            if (normalized.Questions is not null)
            {
                _context.Set<Question>().RemoveRange(quiz.Questions ?? []);
                quiz.Questions = normalized.Questions
                    .Select(question => QuizAdministrationMapper.ToEntity(question, quiz.Id))
                    .ToList();
            }

            quiz.NumberOfQuests = quiz.Questions?.Count ?? 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(quiz), "Ok");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
