using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services.QuizAdministration;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services;

public sealed class QuizAdministrationService : IQuizAdministrationService
{
    private readonly GetQuizzesHandler _getQuizzesHandler;
    private readonly GetQuizByIdHandler _getQuizByIdHandler;
    private readonly CreateQuizHandler _createQuizHandler;
    private readonly UpdateQuizHandler _updateQuizHandler;
    private readonly DeleteQuizHandler _deleteQuizHandler;
    private readonly CreateQuestionHandler _createQuestionHandler;
    private readonly UpdateQuestionHandler _updateQuestionHandler;
    private readonly DeleteQuestionHandler _deleteQuestionHandler;

    public QuizAdministrationService(
        GetQuizzesHandler getQuizzesHandler,
        GetQuizByIdHandler getQuizByIdHandler,
        CreateQuizHandler createQuizHandler,
        UpdateQuizHandler updateQuizHandler,
        DeleteQuizHandler deleteQuizHandler,
        CreateQuestionHandler createQuestionHandler,
        UpdateQuestionHandler updateQuestionHandler,
        DeleteQuestionHandler deleteQuestionHandler)
    {
        _getQuizzesHandler = getQuizzesHandler;
        _getQuizByIdHandler = getQuizByIdHandler;
        _createQuizHandler = createQuizHandler;
        _updateQuizHandler = updateQuizHandler;
        _deleteQuizHandler = deleteQuizHandler;
        _createQuestionHandler = createQuestionHandler;
        _updateQuestionHandler = updateQuestionHandler;
        _deleteQuestionHandler = deleteQuestionHandler;
    }

    public async Task<IReadOnlyList<AdminQuizUpsertDto>> GetQuizzesAsync(int page = 1, int pageSize = 50)
        => await _getQuizzesHandler.HandleAsync(page, pageSize);

    public async Task<AdminQuizUpsertDto?> GetQuizByIdAsync(int quizId)
        => await _getQuizByIdHandler.HandleAsync(quizId);

    public async Task<ServiceResponse<AdminQuizUpsertDto>> CreateQuizAsync(int adminId, AdminQuizUpsertDto request)
        => await _createQuizHandler.HandleAsync(adminId, request);

    public async Task<ServiceResponse<AdminQuizUpsertDto>> UpdateQuizAsync(
        int adminId,
        int quizId,
        AdminQuizUpsertDto request)
        => await _updateQuizHandler.HandleAsync(adminId, quizId, request);

    public async Task<ServiceResponse<bool>> DeleteQuizAsync(int adminId, int quizId)
        => await _deleteQuizHandler.HandleAsync(adminId, quizId);

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> CreateQuestionAsync(
        int adminId,
        int quizId,
        AdminQuestionUpsertDto request)
        => await _createQuestionHandler.HandleAsync(adminId, quizId, request);

    public async Task<ServiceResponse<AdminQuestionUpsertDto>> UpdateQuestionAsync(
        int adminId,
        int questionId,
        AdminQuestionUpsertDto request)
        => await _updateQuestionHandler.HandleAsync(adminId, questionId, request);

    public async Task<ServiceResponse<bool>> DeleteQuestionAsync(int adminId, int questionId)
        => await _deleteQuestionHandler.HandleAsync(adminId, questionId);
}
