using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IQuizCatalogService
{
    Task<IReadOnlyList<QuizCatalogItemDto>> GetPublishedAsync();

    Task<QuizPlayDto?> GetPublishedByIdAsync(int id);

    Task<ServiceResponse<QuizAttemptResultDto>> SubmitAttemptAsync(
        int quizId,
        int userId,
        QuizAttemptRequest request);
}
