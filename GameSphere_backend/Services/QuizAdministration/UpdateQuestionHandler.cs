using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class UpdateQuestionHandler
{
    private readonly AppDbContext _context;
    private readonly QuestionRequestNormalizer _questionRequestNormalizer;

    public UpdateQuestionHandler(AppDbContext context, QuestionRequestNormalizer questionRequestNormalizer)
    {
        _context = context;
        _questionRequestNormalizer = questionRequestNormalizer;
    }

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> HandleAsync(
        int adminId,
        int questionId,
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
            var quizId = await _context.Set<Question>()
                .Where(candidate => candidate.Id == questionId)
                .Select(candidate => (int?)candidate.QuizzId)
                .SingleOrDefaultAsync();
            if (quizId is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>("NotFound", "Question not found.");
            }

            var quiz = await _context.GetQuizForUpdateAsync(quizId.Value);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>("NotFound", "Quiz not found.");
            }

            var question = await _context.Set<Question>().SingleOrDefaultAsync(candidate => candidate.Id == questionId);
            if (question is null)
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<AdminQuestionUpsertDto>("NotFound", "Question not found.");
            }

            question.Description = normalized.Description;
            question.TypeOfAnswer = normalized.TypeOfAnswer;
            question.Answers = normalized.Answers;
            question.CorrectAnswer = normalized.CorrectAnswer;
            await _context.SaveChangesAsync();

            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == question.QuizzId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(question), "Ok");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
