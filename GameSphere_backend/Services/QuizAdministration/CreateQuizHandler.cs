using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class CreateQuizHandler
{
    private readonly AppDbContext _context;
    private readonly QuizRequestNormalizer _quizRequestNormalizer;

    public CreateQuizHandler(AppDbContext context, QuizRequestNormalizer quizRequestNormalizer)
    {
        _context = context;
        _quizRequestNormalizer = quizRequestNormalizer;
    }

    public async Task<ServiceResponse<AdminQuizUpsertDto>> HandleAsync(int adminId, AdminQuizUpsertDto request)
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
            var quiz = new Quizz
            {
                Title = normalized.Title,
                Difficulty = normalized.Difficulty,
                IsPublished = normalized.IsPublished,
                NumberOfQuests = 0,
                RegistrationDate = DateTime.UtcNow,
                UserId = adminId,
                Questions = normalized.Questions?
                    .Select(question => QuizAdministrationMapper.ToEntity(question, 0))
                    .ToList() ?? []
            };

            _context.Quizzs.Add(quiz);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = quiz.Questions?.Count ?? 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(quiz), "Created");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
