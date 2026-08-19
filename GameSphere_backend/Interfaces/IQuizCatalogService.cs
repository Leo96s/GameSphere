using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Interfaces;

public interface IQuizCatalogService
{
    Task<IReadOnlyList<QuizCatalogItemDto>> GetPublishedAsync();

    Task<QuizPlayDto?> GetPublishedByIdAsync(int id);
}
