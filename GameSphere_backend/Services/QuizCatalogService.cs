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

    public QuizCatalogService(AppDbContext context)
    {
        _context = context;
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

        if (quiz is null)
        {
            return Failure("NotFound", "Quiz not found.");
        }

        var questions = quiz.Questions?.ToArray() ?? [];

        if (questions.Length == 0)
        {
            return Failure("BadRequest", "Quiz has no questions.");
        }

        var submittedAnswers = request.Answers;

        if (submittedAnswers.Count != questions.Length)
        {
            return Failure("BadRequest", "An answer is required for every quiz question.");
        }

        var answersByQuestion = new Dictionary<int, QuizAnswerRequest>();
        foreach (var answer in submittedAnswers)
        {
            if (string.IsNullOrWhiteSpace(answer.SelectedAnswer) ||
                !answersByQuestion.TryAdd(answer.QuestionId, answer))
            {
                return Failure("BadRequest", "Each quiz question must have one valid answer.");
            }
        }

        var questionsById = questions.ToDictionary(question => question.Id);
        if (!answersByQuestion.Keys.ToHashSet().SetEquals(questionsById.Keys))
        {
            return Failure("BadRequest", "The submitted answers do not match this quiz.");
        }

        foreach (var question in questions)
        {
            var selectedAnswer = answersByQuestion[question.Id].SelectedAnswer;
            if (!question.Answers.Contains(selectedAnswer, StringComparer.Ordinal))
            {
                return Failure("BadRequest", "Each selected answer must be one of the question options.");
            }
        }

        var correctAnswers = questions.Count(question =>
            string.Equals(
                question.CorrectAnswer,
                answersByQuestion[question.Id].SelectedAnswer,
                StringComparison.Ordinal));

        _context.Scores.Add(new Score
        {
            UserId = userId,
            QuizzId = quiz.Id,
            GameId = null,
            Date = DateTime.UtcNow,
            Points = correctAnswers
        });
        await _context.SaveChangesAsync();

        return new ServiceResponse<QuizAttemptResultDto>
        {
            Success = true,
            Type = "Ok",
            Data = new QuizAttemptResultDto
            {
                CorrectAnswers = correctAnswers,
                TotalQuestions = questions.Length,
                Percentage = questions.Length == 0
                    ? 0m
                    : correctAnswers * 100m / questions.Length
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
