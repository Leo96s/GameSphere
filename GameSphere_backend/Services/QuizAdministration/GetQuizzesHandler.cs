using GameSphere_backend.Data;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

public sealed class GetQuizzesHandler
{
    private readonly AppDbContext _context;

    public GetQuizzesHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminQuizUpsertDto>> HandleAsync(int page = 1, int pageSize = 50)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var quizzes = await _context.Quizzs
            .AsNoTracking()
            .Include(quiz => quiz.Questions)
            .OrderBy(quiz => quiz.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return quizzes.Select(QuizAdministrationMapper.ToDto).ToArray();
    }
}
