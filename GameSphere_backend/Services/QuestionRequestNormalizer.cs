using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public sealed class QuestionRequestNormalizer
{
    private readonly IQuestionShapeValidator _shapeValidator;
    private readonly IQuestionAnswersValidator _answersValidator;
    private readonly ICorrectAnswerValidator _correctAnswerValidator;

    public QuestionRequestNormalizer(
        IQuestionShapeValidator shapeValidator,
        IQuestionAnswersValidator answersValidator,
        ICorrectAnswerValidator correctAnswerValidator)
    {
        _shapeValidator = shapeValidator;
        _answersValidator = answersValidator;
        _correctAnswerValidator = correctAnswerValidator;
    }

    public bool TryNormalize(
        AdminQuestionUpsertDto? request,
        out AdminQuestionUpsertDto normalized,
        out string message)
    {
        normalized = new AdminQuestionUpsertDto();
        var shapeMessage = _shapeValidator.Validate(request);
        if (shapeMessage is not null)
        {
            message = shapeMessage;
            return false;
        }

        var candidate = request!;
        var answersMessage = _answersValidator.ValidateAndNormalize(candidate, out var answers);
        if (answersMessage is not null)
        {
            message = answersMessage;
            return false;
        }

        var correctAnswer = candidate.CorrectAnswer.Trim();
        var correctAnswerMessage = _correctAnswerValidator.Validate(answers, correctAnswer);
        if (correctAnswerMessage is not null)
        {
            message = correctAnswerMessage;
            return false;
        }

        normalized = new AdminQuestionUpsertDto
        {
            Id = candidate.Id,
            Description = candidate.Description.Trim(),
            TypeOfAnswer = candidate.TypeOfAnswer,
            Answers = answers,
            CorrectAnswer = correctAnswer,
            QuizzId = candidate.QuizzId
        };
        message = string.Empty;
        return true;
    }
}
