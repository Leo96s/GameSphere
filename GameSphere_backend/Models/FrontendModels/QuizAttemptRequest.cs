using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class QuizAttemptRequest
{
    [Required]
    public required IReadOnlyList<QuizAnswerRequest> Answers { get; init; } = [];
}
