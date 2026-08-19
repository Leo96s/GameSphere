using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class QuizCatalogItemDto
{
    public int Id { get; init; }

    public required string Title { get; init; }

    public Difficulty Difficulty { get; init; }

    public int NumberOfQuests { get; init; }
}
