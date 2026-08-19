using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services;

public sealed class QuizCatalogService : IQuizCatalogService
{
    private readonly AppDbContext _context;

    public QuizCatalogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<QuizCatalogItemDto>> GetPublishedAsync()
    {
        var quizzes = await _context.Quizzs
            .AsNoTracking()
            .Where(quiz => quiz.IsPublished)
            .OrderBy(quiz => quiz.Id)
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
}
