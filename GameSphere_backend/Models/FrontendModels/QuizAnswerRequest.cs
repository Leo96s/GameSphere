using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class QuizAnswerRequest
{
    public int QuestionId { get; init; }

    [Required]
    public required string SelectedAnswer { get; init; }
}
