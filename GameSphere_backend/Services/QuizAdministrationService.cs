using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameSphere_backend.Services;

public sealed class QuizAdministrationService : IQuizAdministrationService
{
    private readonly AppDbContext _context;

    public QuizAdministrationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminQuizUpsertDto>> GetQuizzesAsync()
    {
        var quizzes = await _context.Quizzs
            .AsNoTracking()
            .Include(quiz => quiz.Questions)
            .OrderBy(quiz => quiz.Id)
            .ToListAsync();

        return quizzes.Select(ToDto).ToArray();
    }

    public async Task<AdminQuizUpsertDto?> GetQuizByIdAsync(int quizId)
    {
        var quiz = await _context.Quizzs
            .AsNoTracking()
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == quizId);

        return quiz is null ? null : ToDto(quiz);
    }

    public async Task<ServiceResponse<AdminQuizUpsertDto>> CreateQuizAsync(int adminId, AdminQuizUpsertDto request)
    {
        if (!TryNormalizeQuiz(request, out var normalized, out var message))
        {
            return Failure<AdminQuizUpsertDto>("BadRequest", message);
        }

        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<AdminQuizUpsertDto>("NotFound", "The authenticated administrator was not found.");
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
                Questions = normalized.Questions?.Select(question => ToEntity(question, 0)).ToList() ?? []
            };

            _context.Quizzs.Add(quiz);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = quiz.Questions?.Count ?? 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(ToDto(quiz), "Created");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResponse<AdminQuizUpsertDto>> UpdateQuizAsync(
        int adminId,
        int quizId,
        AdminQuizUpsertDto request)
    {
        if (!TryNormalizeQuiz(request, out var normalized, out var message))
        {
            return Failure<AdminQuizUpsertDto>("BadRequest", message);
        }

        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<AdminQuizUpsertDto>("NotFound", "The authenticated administrator was not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return Failure<AdminQuizUpsertDto>("NotFound", "Quiz not found.");
            }

            await _context.Entry(quiz).Collection(candidate => candidate.Questions!).LoadAsync();

            quiz.Title = normalized.Title;
            quiz.Difficulty = normalized.Difficulty;
            quiz.IsPublished = normalized.IsPublished;

            if (normalized.Questions is not null)
            {
                _context.Set<Question>().RemoveRange(quiz.Questions ?? []);
                quiz.Questions = normalized.Questions
                    .Select(question => ToEntity(question, quiz.Id))
                    .ToList();
            }

            quiz.NumberOfQuests = quiz.Questions?.Count ?? 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(ToDto(quiz), "Ok");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResponse<bool>> DeleteQuizAsync(int adminId, int quizId)
    {
        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<bool>("NotFound", "The authenticated administrator was not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return Failure<bool>("NotFound", "Quiz not found.");
            }

            if (await _context.Scores.AnyAsync(score => score.QuizzId == quizId))
            {
                await transaction.RollbackAsync();
                return Failure<bool>("Conflict", "A quiz with existing attempts cannot be deleted.");
            }

            await _context.Entry(quiz).Collection(candidate => candidate.Questions!).LoadAsync();
            _context.Set<Question>().RemoveRange(quiz.Questions ?? []);
            _context.Quizzs.Remove(quiz);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(true, "NoContent");
        }
        catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync();
            return Failure<bool>("Conflict", "The quiz could not be deleted because of related data.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> CreateQuestionAsync(
        int adminId,
        int quizId,
        AdminQuestionUpsertDto request)
    {
        if (!TryNormalizeQuestion(request, out var normalized, out var message))
        {
            return Failure<AdminQuestionUpsertDto>("BadRequest", message);
        }

        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<AdminQuestionUpsertDto>("NotFound", "The authenticated administrator was not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quiz = await GetQuizForUpdateAsync(quizId);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return Failure<AdminQuestionUpsertDto>("NotFound", "Quiz not found.");
            }

            var question = ToEntity(normalized, quizId);
            _context.Set<Question>().Add(question);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == quizId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(ToDto(question), "Created");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> UpdateQuestionAsync(
        int adminId,
        int questionId,
        AdminQuestionUpsertDto request)
    {
        if (!TryNormalizeQuestion(request, out var normalized, out var message))
        {
            return Failure<AdminQuestionUpsertDto>("BadRequest", message);
        }

        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<AdminQuestionUpsertDto>("NotFound", "The authenticated administrator was not found.");
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
                return Failure<AdminQuestionUpsertDto>("NotFound", "Question not found.");
            }

            var quiz = await GetQuizForUpdateAsync(quizId.Value);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return Failure<AdminQuestionUpsertDto>("NotFound", "Quiz not found.");
            }

            var question = await _context.Set<Question>().SingleOrDefaultAsync(candidate => candidate.Id == questionId);
            if (question is null)
            {
                await transaction.RollbackAsync();
                return Failure<AdminQuestionUpsertDto>("NotFound", "Question not found.");
            }

            question.Description = normalized.Description;
            question.TypeOfAnswer = normalized.TypeOfAnswer;
            question.Answers = normalized.Answers;
            question.CorrectAnswer = normalized.CorrectAnswer;
            await _context.SaveChangesAsync();

            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == question.QuizzId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(ToDto(question), "Ok");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResponse<bool>> DeleteQuestionAsync(int adminId, int questionId)
    {
        if (!await IsPersistedAdminAsync(adminId))
        {
            return Failure<bool>("NotFound", "The authenticated administrator was not found.");
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
                return Failure<bool>("NotFound", "Question not found.");
            }

            var quiz = await GetQuizForUpdateAsync(quizId.Value);
            if (quiz is null)
            {
                await transaction.RollbackAsync();
                return Failure<bool>("NotFound", "Quiz not found.");
            }

            var question = await _context.Set<Question>().SingleOrDefaultAsync(candidate => candidate.Id == questionId);
            if (question is null)
            {
                await transaction.RollbackAsync();
                return Failure<bool>("NotFound", "Question not found.");
            }

            _context.Set<Question>().Remove(question);
            await _context.SaveChangesAsync();
            quiz.NumberOfQuests = await _context.Set<Question>().CountAsync(candidate => candidate.QuizzId == quiz.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Success(true, "NoContent");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> IsPersistedAdminAsync(int adminId) => await _context.Users
        .AnyAsync(user => user.Id == adminId && user.Role == UserRole.Admin);

    private async Task<Quizz?> GetQuizForUpdateAsync(int quizId) => await _context.Quizzs
        .FromSqlInterpolated($"SELECT * FROM \"Quizzs\" WHERE \"Id\" = {quizId} FOR UPDATE")
        .SingleOrDefaultAsync();

    private static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };

    private static bool TryNormalizeQuiz(
        AdminQuizUpsertDto? request,
        out AdminQuizUpsertDto normalized,
        out string message)
    {
        normalized = new AdminQuizUpsertDto();
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            message = "A quiz title is required.";
            return false;
        }

        var title = request.Title.Trim();
        if (title.Length > 100 || !Enum.IsDefined(request.Difficulty))
        {
            message = "The quiz contains an invalid title or difficulty.";
            return false;
        }

        List<AdminQuestionUpsertDto>? questions = null;
        if (request.Questions is not null)
        {
            questions = [];
            foreach (var question in request.Questions)
            {
                if (!TryNormalizeQuestion(question, out var normalizedQuestion, out message))
                {
                    return false;
                }

                questions.Add(normalizedQuestion);
            }
        }

        normalized = new AdminQuizUpsertDto
        {
            Title = title,
            Difficulty = request.Difficulty,
            IsPublished = request.IsPublished,
            Questions = questions
        };
        message = string.Empty;
        return true;
    }

    private static bool TryNormalizeQuestion(
        AdminQuestionUpsertDto? request,
        out AdminQuestionUpsertDto normalized,
        out string message)
    {
        normalized = new AdminQuestionUpsertDto();
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.CorrectAnswer) ||
            request.Answers is null ||
            request.Answers.Length < 2 ||
            !Enum.IsDefined(request.TypeOfAnswer))
        {
            message = "A question, supported answer type, options and correct answer are required.";
            return false;
        }

        var answers = request.Answers.Select(answer => answer?.Trim() ?? string.Empty).ToArray();
        if (answers.Any(string.IsNullOrWhiteSpace) ||
            answers.Distinct(StringComparer.Ordinal).Count() != answers.Length)
        {
            message = "Each question option must be unique and non-empty.";
            return false;
        }

        var correctAnswer = request.CorrectAnswer.Trim();
        if (!answers.Contains(correctAnswer, StringComparer.Ordinal))
        {
            message = "The correct answer must be one of the question options.";
            return false;
        }

        normalized = new AdminQuestionUpsertDto
        {
            Description = request.Description.Trim(),
            TypeOfAnswer = request.TypeOfAnswer,
            Answers = answers,
            CorrectAnswer = correctAnswer
        };
        message = string.Empty;
        return true;
    }

    private static Question ToEntity(AdminQuestionUpsertDto source, int quizId) => new()
    {
        Description = source.Description,
        TypeOfAnswer = source.TypeOfAnswer,
        Answers = source.Answers,
        CorrectAnswer = source.CorrectAnswer,
        QuizzId = quizId
    };

    private static AdminQuizUpsertDto ToDto(Quizz source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Difficulty = source.Difficulty,
        NumberOfQuests = source.NumberOfQuests,
        IsPublished = source.IsPublished,
        Questions = source.Questions?
            .OrderBy(question => question.Id)
            .Select(ToDto)
            .ToList() ?? []
    };

    private static AdminQuestionUpsertDto ToDto(Question source) => new()
    {
        Id = source.Id,
        Description = source.Description,
        TypeOfAnswer = source.TypeOfAnswer,
        Answers = source.Answers,
        CorrectAnswer = source.CorrectAnswer,
        QuizzId = source.QuizzId
    };

    private static ServiceResponse<T> Success<T>(T data, string type) => new()
    {
        Success = true,
        Type = type,
        Data = data
    };

    private static ServiceResponse<T> Failure<T>(string type, string message) => new()
    {
        Success = false,
        Type = type,
        Message = message
    };
}
