using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class AdminQuestionUpsertDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public TypeOfAnswer TypeOfAnswer { get; set; }

    [Required]
    [MinLength(2), MaxLength(6)]
    public string[] Answers { get; set; } = [];

    [Required]
    [StringLength(500)]
    public string CorrectAnswer { get; set; } = string.Empty;

    public int QuizzId { get; set; }
}
