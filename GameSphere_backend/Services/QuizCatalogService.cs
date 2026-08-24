using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

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
            .Where(quiz => quiz.IsPublished)
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
            .SingleOrDefaultAsync(quiz => quiz.Id == id && quiz.IsPublished);

        return quiz is null ? null : QuizPlayerMapper.ToPlayQuiz(quiz);
    }

    public async Task<ServiceResponse<QuizAttemptResultDto>> SubmitAttemptAsync(
        int quizId,
        int userId,
        QuizAttemptRequest request)
    {
        var quiz = await _context.Quizzs
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == quizId && candidate.IsPublished);

        var validation = _attemptValidator.Validate(quiz, request);
        if (!validation.Success)
        {
            return Failure(validation.Type!, validation.Message!);
        }

        var attempt = validation.Data!;
        var correctAnswers = _attemptScorer.CountCorrectAnswers(attempt);

        await _attemptPersistenceService.SaveScoreAsync(userId, quiz!.Id, correctAnswers);

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

    private static ServiceResponse<QuizAttemptResultDto> Failure(string type, string message) => new()
    {
        Success = false,
        Type = type,
        Message = message
    };
}
