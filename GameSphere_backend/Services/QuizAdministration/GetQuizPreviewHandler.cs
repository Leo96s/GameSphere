using GameSphere_backend.Data;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class GetQuizPreviewHandler
{
    private readonly AppDbContext _context;

    public GetQuizPreviewHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<QuizPlayDto>> HandleAsync(int quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _context.Quizzs
            .AsNoTracking()
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == quizId, cancellationToken);

        if (quiz is null)
        {
            return QuizAdministrationResponses.Failure<QuizPlayDto>("NotFound", "Quiz not found.");
        }

        quiz.Questions = (quiz.Questions ?? []).OrderBy(question => question.Id).ToList();

        return QuizAdministrationResponses.Success(QuizPlayerMapper.ToPlayQuiz(quiz), "Success");
    }
}
