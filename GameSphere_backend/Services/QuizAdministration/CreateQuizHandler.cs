using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

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

        const string duplicateKeyMessage = "A quiz with this content key already exists.";
        if (normalized.ContentKey is not null
            && await _context.Quizzs.AnyAsync(candidate => candidate.ContentKey == normalized.ContentKey))
        {
            return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>("Conflict", duplicateKeyMessage);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = new Quizz
            {
                Title = normalized.Title,
                Difficulty = normalized.Difficulty,
                Category = normalized.Category!.Value,
                ContentKey = normalized.ContentKey,
                Status = QuizStatus.Draft,
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

            return QuizAdministrationResponses.Success(QuizAdministrationMapper.ToDto(quiz, hasAttempts: false), "Created");
        }
        catch (DbUpdateException exception) when (QuizAdministrationDataGuards.IsContentKeyUniqueViolation(exception))
        {
            await transaction.RollbackAsync();
            return QuizAdministrationResponses.Failure<AdminQuizUpsertDto>("Conflict", duplicateKeyMessage);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
