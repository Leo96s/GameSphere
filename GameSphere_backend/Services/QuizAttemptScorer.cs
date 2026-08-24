using GameSphere_backend.Models.BackendModels;

namespace GameSphere_backend.Services;

public sealed class QuizAttemptScorer
{
    internal int CountCorrectAnswers(ValidatedQuizAttempt attempt)
    {
        return attempt.Questions.Count(question => IsCorrectAnswer(question, attempt));
    }

    private static bool IsCorrectAnswer(Question question, ValidatedQuizAttempt attempt)
    {
        return string.Equals(
            question.CorrectAnswer,
            attempt.AnswersByQuestion[question.Id].SelectedAnswer,
            StringComparison.Ordinal);
    }
}
