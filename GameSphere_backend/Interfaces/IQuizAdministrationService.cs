using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IQuizAdministrationService
{
    Task<IReadOnlyList<AdminQuizUpsertDto>> GetQuizzesAsync();

    Task<AdminQuizUpsertDto?> GetQuizByIdAsync(int quizId);

    Task<ServiceResponse<AdminQuizUpsertDto>> CreateQuizAsync(int adminId, AdminQuizUpsertDto request);

    Task<ServiceResponse<AdminQuizUpsertDto>> UpdateQuizAsync(
        int adminId,
        int quizId,
        AdminQuizUpsertDto request);

    Task<ServiceResponse<bool>> DeleteQuizAsync(int adminId, int quizId);

    Task<ServiceResponse<AdminQuestionUpsertDto>> CreateQuestionAsync(
        int adminId,
        int quizId,
        AdminQuestionUpsertDto request);

    Task<ServiceResponse<AdminQuestionUpsertDto>> UpdateQuestionAsync(
        int adminId,
        int questionId,
        AdminQuestionUpsertDto request);

    Task<ServiceResponse<bool>> DeleteQuestionAsync(int adminId, int questionId);
}
