namespace GameSphere_backend.Models.FrontendModels;

public sealed class QuizAttemptResultDto
{
    public int CorrectAnswers { get; init; }

    public int TotalQuestions { get; init; }

    public decimal Percentage { get; init; }
}
