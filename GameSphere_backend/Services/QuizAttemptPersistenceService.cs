using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;

namespace GameSphere_backend.Services;

public sealed class QuizAttemptPersistenceService
{
    private readonly AppDbContext _context;

    public QuizAttemptPersistenceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveScoreAsync(int userId, int quizId, int correctAnswers)
    {
        _context.Scores.Add(new Score
        {
            UserId = userId,
            QuizzId = quizId,
            GameId = null,
            Date = DateTime.UtcNow,
            Points = correctAnswers
        });

        await _context.SaveChangesAsync();
    }
}
