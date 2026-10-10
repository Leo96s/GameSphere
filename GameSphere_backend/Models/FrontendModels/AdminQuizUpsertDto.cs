using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class AdminQuizUpsertDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; }

    public int NumberOfQuests { get; set; }

    /// <summary>
    /// Output only: the editorial status is ignored on create/update and changes only through the status endpoint.
    /// </summary>
    public QuizStatus Status { get; set; }

    /// <summary>
    /// Output only: whether the quiz already has attempts. Ignored on create/update.
    /// </summary>
    public bool HasAttempts { get; set; }

    public List<AdminQuestionUpsertDto>? Questions { get; set; }
}
