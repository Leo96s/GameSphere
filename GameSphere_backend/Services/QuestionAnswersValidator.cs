using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public interface IQuestionAnswersValidator
{
    string? ValidateAndNormalize(AdminQuestionUpsertDto request, out string[] answers);
}

public sealed class QuestionAnswersValidator : IQuestionAnswersValidator
{
    public string? ValidateAndNormalize(AdminQuestionUpsertDto request, out string[] answers)
    {
        answers = request.Answers.Select(answer => answer?.Trim() ?? string.Empty).ToArray();
        if (answers.Any(answer => string.IsNullOrWhiteSpace(answer) || answer.Length > 500) ||
            answers.Distinct(StringComparer.Ordinal).Count() != answers.Length)
        {
            return "Each question option must be unique and non-empty.";
        }

        return null;
    }
}
