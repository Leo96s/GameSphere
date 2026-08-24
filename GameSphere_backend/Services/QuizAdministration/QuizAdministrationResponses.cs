using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services.QuizAdministration;

internal static class QuizAdministrationResponses
{
    public static ServiceResponse<T> Success<T>(T data, string type) => new()
    {
        Success = true,
        Type = type,
        Data = data
    };

    public static ServiceResponse<T> Failure<T>(string type, string message) => new()
    {
        Success = false,
        Type = type,
        Message = message
    };
}
