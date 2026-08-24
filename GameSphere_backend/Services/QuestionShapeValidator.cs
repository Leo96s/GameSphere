using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public interface IQuestionShapeValidator
{
    string? Validate(AdminQuestionUpsertDto? request);
}

public sealed class QuestionShapeValidator : IQuestionShapeValidator
{
    public string? Validate(AdminQuestionUpsertDto? request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.CorrectAnswer) ||
            request.Answers is null ||
            request.Answers.Length < 2 ||
            !Enum.IsDefined(request.TypeOfAnswer))
        {
            return "A question, supported answer type, options and correct answer are required.";
        }

        if (request.Description.Trim().Length > 2_000 || request.Answers.Length > 6)
        {
            return "A question or its options exceed the allowed size.";
        }

        return null;
    }
}
