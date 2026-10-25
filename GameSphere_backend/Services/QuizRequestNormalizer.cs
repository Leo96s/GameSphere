using System.Text.RegularExpressions;
using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public sealed partial class QuizRequestNormalizer
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*\\z", RegexOptions.CultureInvariant)]
    private static partial Regex ContentKeyPattern();

    private readonly QuestionRequestNormalizer _questionRequestNormalizer;
    private readonly IEditorialTextValidator _textValidator;

    public QuizRequestNormalizer(
        QuestionRequestNormalizer questionRequestNormalizer,
        IEditorialTextValidator textValidator)
    {
        _questionRequestNormalizer = questionRequestNormalizer;
        _textValidator = textValidator;
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
        var titleMessage = _textValidator.Validate(title, allowLineBreaks: false);
        if (titleMessage is not null)
        {
            message = titleMessage;
            return false;
        }

        if (title.Length > 100 || !Enum.IsDefined(request.Difficulty))
        {
            message = "The quiz contains an invalid title or difficulty.";
            return false;
        }

        if (request.Category is not { } category || !Enum.IsDefined(category))
        {
            message = "The quiz category is invalid.";
            return false;
        }

        if (request.ContentKey is not null
            && (request.ContentKey.Length > 100 || !ContentKeyPattern().IsMatch(request.ContentKey)))
        {
            message = "The quiz content key is invalid.";
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
            Category = category,
            ContentKey = request.ContentKey,
            NumberOfQuests = request.NumberOfQuests,
            Questions = questions
        };
        message = string.Empty;
        return true;
    }
}
