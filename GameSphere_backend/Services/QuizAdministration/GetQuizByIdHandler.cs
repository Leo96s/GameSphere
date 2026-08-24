using GameSphere_backend.Data;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class GetQuizByIdHandler
{
    private readonly AppDbContext _context;

    public GetQuizByIdHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminQuizUpsertDto?> HandleAsync(int quizId)
    {
        var quiz = await _context.Quizzs
            .AsNoTracking()
            .Include(candidate => candidate.Questions)
            .SingleOrDefaultAsync(candidate => candidate.Id == quizId);

        return quiz is null ? null : QuizAdministrationMapper.ToDto(quiz);
    }
}
