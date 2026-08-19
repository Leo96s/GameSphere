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

    public bool IsPublished { get; set; }

    public List<AdminQuestionUpsertDto>? Questions { get; set; }
}
