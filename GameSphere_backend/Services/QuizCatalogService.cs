using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services.QuizAdministration;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameSphere_backend.Services;

public sealed class QuizCatalogService : IQuizCatalogService
{
    private readonly AppDbContext _context;
    private readonly QuizAttemptValidator _attemptValidator;
    private readonly QuizAttemptScorer _attemptScorer;
    private readonly QuizAttemptPersistenceService _attemptPersistenceService;

    public QuizCatalogService(
        AppDbContext context,
        QuizAttemptValidator attemptValidator,
        QuizAttemptScorer attemptScorer,
        QuizAttemptPersistenceService attemptPersistenceService)
    {
        _context = context;
        _attemptValidator = attemptValidator;
        _attemptScorer = attemptScorer;
        _attemptPersistenceService = attemptPersistenceService;
    }

    public async Task<IReadOnlyList<QuizCatalogItemDto>> GetPublishedAsync(int page = 1, int pageSize = 50)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var quizzes = await _context.Quizzs
            .AsNoTracking()
            .Where(quiz => quiz.Status == QuizStatus.Published)
            .OrderBy(quiz => quiz.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return quizzes.Select(QuizPlayerMapper.ToCatalogItem).ToArray();
    }

    public async Task<QuizPlayDto?> GetPublishedByIdAsync(int id)
    {
        var quiz = await _context.Quizzs
            .AsNoTracking()
            .Include(quiz => quiz.Questions)
            .SingleOrDefaultAsync(quiz => quiz.Id == id && quiz.Status == QuizStatus.Published);

        return quiz is null ? null : QuizPlayerMapper.ToPlayQuiz(quiz);
    }

    public async Task<ServiceResponse<QuizAttemptResultDto>> SubmitAttemptAsync(
        int quizId,
        int userId,
        QuizAttemptRequest request)
    {
        // The shared row lock serialises the attempt with administrative changes (FOR UPDATE) on the same quiz:
        // either the score is saved first and the content guard sees it, or the attempt waits and re-reads the status.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        int correctAnswers;
        ValidatedQuizAttempt attempt;
        try
        {
            // Bounded wait: if an admin operation holds FOR UPDATE for too long the attempt fails with 409 instead of hanging.
            await _context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
            await _context.GetQuizForShareAsync(quizId);

            var quiz = await _context.Quizzs
                .Include(candidate => candidate.Questions)
                .SingleOrDefaultAsync(candidate => candidate.Id == quizId && candidate.Status == QuizStatus.Published);

            var validation = _attemptValidator.Validate(quiz, request);
            if (!validation.Success)
            {
                await transaction.RollbackAsync();
                return Failure(validation.Type!, validation.Message!);
            }

            attempt = validation.Data!;
            correctAnswers = _attemptScorer.CountCorrectAnswers(attempt);

            await _attemptPersistenceService.SaveScoreAsync(userId, quiz!.Id, correctAnswers);
            await transaction.CommitAsync();
        }
        catch (Exception exception) when (IsLockNotAvailable(exception))
        {
            await transaction.RollbackAsync();
            return Failure("Conflict", "The quiz is being updated. Please try again.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new ServiceResponse<QuizAttemptResultDto>
        {
            Success = true,
            Type = "Ok",
            Data = new QuizAttemptResultDto
            {
                CorrectAnswers = correctAnswers,
                TotalQuestions = attempt.Questions.Count,
                Percentage = attempt.Questions.Count == 0
                    ? 0m
                    : correctAnswers * 100m / attempt.Questions.Count
            }
        };
    }

    private static bool IsLockNotAvailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable })
            {
                return true;
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return false;
    }

    private static ServiceResponse<QuizAttemptResultDto> Failure(string type, string message) => new()
    {
        Success = false,
        Type = type,
        Message = message
    };
}
