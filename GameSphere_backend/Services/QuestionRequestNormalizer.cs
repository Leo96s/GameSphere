using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services;

public sealed class QuestionRequestNormalizer
{
    private readonly IQuestionShapeValidator _shapeValidator;
    private readonly IQuestionAnswersValidator _answersValidator;
    private readonly ICorrectAnswerValidator _correctAnswerValidator;
    private readonly IEditorialTextValidator _textValidator;

    public QuestionRequestNormalizer(
        IQuestionShapeValidator shapeValidator,
        IQuestionAnswersValidator answersValidator,
        ICorrectAnswerValidator correctAnswerValidator,
        IEditorialTextValidator textValidator)
    {
        _shapeValidator = shapeValidator;
        _answersValidator = answersValidator;
        _correctAnswerValidator = correctAnswerValidator;
        _textValidator = textValidator;
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
        var textMessage = _textValidator.Validate(candidate.Description, allowLineBreaks: true)
            ?? candidate.Answers
                .Select(answer => _textValidator.Validate(answer, allowLineBreaks: false))
                .FirstOrDefault(result => result is not null)
            ?? _textValidator.Validate(candidate.CorrectAnswer, allowLineBreaks: false);
        if (textMessage is not null)
        {
            message = textMessage;
            return false;
        }

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
