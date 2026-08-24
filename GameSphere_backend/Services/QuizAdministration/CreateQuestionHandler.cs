using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class CreateQuestionHandler
{
    private readonly AppDbContext _context;
    private readonly QuestionRequestNormalizer _questionRequestNormalizer;

    public CreateQuestionHandler(AppDbContext context, QuestionRequestNormalizer questionRequestNormalizer)
    {
        _context = context;
        _questionRequestNormalizer = questionRequestNormalizer;
    }

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> HandleAsync(
        int adminId,
        int quizId,
        AdminQuestionUpsertDto request)
    {
        if (!_questionRequestNormalizer.TryNormalize(request, out var normalized, out var message))
        {
            return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>("BadRequest", message);
        }

        if (!await _context.IsPersistedAdminAsync(adminId))
        {
            return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>(
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
                return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>("NotFound", "Quiz not found.");
            }

            var question = QuizAdministrationMapper.ToEntity(normalized, quizId);
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == quizId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(question), "Created");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
