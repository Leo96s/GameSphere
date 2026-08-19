using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class AdminQuestionUpsertDto
{
    public int Id { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public TypeOfAnswer TypeOfAnswer { get; set; }

    [Required]
    public string[] Answers { get; set; } = [];

    [Required]
    public string CorrectAnswer { get; set; } = string.Empty;

    public int QuizzId { get; set; }
}
