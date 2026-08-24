using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public sealed class QuizRequestNormalizer
{
    private readonly QuestionRequestNormalizer _questionRequestNormalizer;

    public QuizRequestNormalizer(QuestionRequestNormalizer questionRequestNormalizer)
    {
        _questionRequestNormalizer = questionRequestNormalizer;
    }

    public bool TryNormalize(
        AdminQuizUpsertDto? request,
        out AdminQuizUpsertDto normalized,
        out string message)
    {
        normalized = new AdminQuizUpsertDto();
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            message = "A quiz title is required.";
            return false;
        }

        var title = request.Title.Trim();
        if (title.Length > 100 || !Enum.IsDefined(request.Difficulty))
        {
            message = "The quiz contains an invalid title or difficulty.";
            return false;
        }

        if (request.Questions?.Count > 100)
        {
            message = "A quiz cannot contain more than 100 questions.";
            return false;
        }

        List<AdminQuestionUpsertDto>? questions = null;
        if (request.Questions is not null)
        {
            questions = [];
            foreach (var question in request.Questions)
            {
                if (!_questionRequestNormalizer.TryNormalize(question, out var normalizedQuestion, out message))
                {
                    return false;
                }

                questions.Add(normalizedQuestion);
            }
        }

        normalized = new AdminQuizUpsertDto
        {
            Id = request.Id,
            Title = title,
            Difficulty = request.Difficulty,
            NumberOfQuests = request.NumberOfQuests,
            IsPublished = request.IsPublished,
            Questions = questions
        };
        message = string.Empty;
        return true;
    }
}
