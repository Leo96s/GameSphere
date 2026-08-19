using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class QuizQuestionDto
{
    public int Id { get; init; }

    public required string Description { get; init; }

    public TypeOfAnswer TypeOfAnswer { get; init; }

    public required string[] Answers { get; init; }
}
