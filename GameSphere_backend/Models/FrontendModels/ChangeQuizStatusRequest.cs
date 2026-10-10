using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels;

public sealed class ChangeQuizStatusRequest
{
    public QuizStatus? Status { get; set; }
}
