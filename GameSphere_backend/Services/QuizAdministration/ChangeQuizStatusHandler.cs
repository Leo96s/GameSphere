using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class ChangeQuizStatusHandler
{
    private readonly AppDbContext _context;
    private readonly IQuizPublicationValidator _publicationValidator;

    public ChangeQuizStatusHandler(AppDbContext context, IQuizPublicationValidator publicationValidator)
    {
        _context = context;
        _publicationValidator = publicationValidator;
    }

    public async Task<ServiceResponse<AdminQuizUpsertDto>> HandleAsync(
        int adminId,
        int quizId,
        ChangeQuizStatusRequest? request)
    {
        if (request?.Status is not { } targetStatus || !Enum.IsDefined(targetStatus))
        {
            return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>(
                "BadRequest",
                "A valid quiz status is required.");
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

            if (!QuizStatusTransitions.IsAllowed(quiz.Status, targetStatus))
            {
                await transaction.RollbackAsync();
                return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>(
                    "Conflict",
                    $"Cannot change quiz status from {quiz.Status} to {targetStatus}.");
            }

            await _context.Entry(quiz).Collection(candidate => candidate.Questions!).LoadAsync();

            if (targetStatus == QuizStatus.Published)
            {
                var publicationMessage = _publicationValidator.Validate(quiz);
                if (publicationMessage is not null)
                {
                    await transaction.RollbackAsync();
                    return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>("BadRequest", publicationMessage);
                }
            }

            var hasAttempts = await _context.HasAttemptsAsync(quiz.Id);
            quiz.Status = targetStatus;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(quiz, hasAttempts), "Ok");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
