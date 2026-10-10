using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services.QuizAdministration;

internal static class QuizContentEditGuard
{
    public const string NotEditableStatusMessage = "Only draft or unpublished quizzes can be edited.";
    public const string ExistingAttemptsMessage = "A quiz with existing attempts cannot have its content changed.";

    /// <summary>
    /// Returns the failure message when the quiz content cannot be changed, or null when it can.
    /// Must be called after the quiz row has been locked.
    /// </summary>
    public static async Task<string?> GetContentEditFailureAsync(this AppDbContext context, Quizz quiz)
    {
        if (quiz.Status is not (QuizStatus.Draft or QuizStatus.Unpublished))
        {
            return NotEditableStatusMessage;
        }

        if (await context.Scores.AnyAsync(score => score.QuizzId == quiz.Id))
        {
            return ExistingAttemptsMessage;
        }

        return null;
    }
}
