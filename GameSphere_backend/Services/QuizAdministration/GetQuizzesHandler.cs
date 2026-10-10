using GameSphere_backend.Data;
using GameSphere_backend.Enums;
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

    public async Task<IReadOnlyList<AdminQuizUpsertDto>> HandleAsync(
        int page = 1,
        int pageSize = 50,
        bool includeArchived = false)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await _context.Quizzs
            .AsNoTracking()
            .Include(quiz => quiz.Questions)
            .Where(quiz => includeArchived || quiz.Status != QuizStatus.Archived)
            .OrderBy(quiz => quiz.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(quiz => new
            {
                Quiz = quiz,
                HasAttempts = _context.Scores.Any(score => score.QuizzId == quiz.Id)
            })
            .ToListAsync();

        return rows.Select(row => QuizAdministrationMapper.ToDto(row.Quiz, row.HasAttempts)).ToArray();
    }
}
