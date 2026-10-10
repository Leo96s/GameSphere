using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services.QuizAdministration;

namespace GameSphere_backend.Services;

public interface IQuizPublicationValidator
{
    string? Validate(Quizz quiz);
}

public sealed class QuizPublicationValidator : IQuizPublicationValidator
{
    private readonly IQuestionShapeValidator _shapeValidator;
    private readonly IQuestionAnswersValidator _answersValidator;
    private readonly ICorrectAnswerValidator _correctAnswerValidator;

    public QuizPublicationValidator(
        IQuestionShapeValidator shapeValidator,
        IQuestionAnswersValidator answersValidator,
        ICorrectAnswerValidator correctAnswerValidator)
    {
        _shapeValidator = shapeValidator;
        _answersValidator = answersValidator;
        _correctAnswerValidator = correctAnswerValidator;
    }

    public string? Validate(Quizz quiz)
    {
        if (string.IsNullOrWhiteSpace(quiz.Title))
        {
            return "A quiz title is required.";
        }

        if (quiz.Title.Trim().Length > 100)
        {
            return "The quiz title cannot exceed 100 characters.";
        }

        if (!Enum.IsDefined(quiz.Difficulty))
        {
            return "The quiz difficulty is invalid.";
        }

        var questions = quiz.Questions?.OrderBy(question => question.Id).ToList() ?? [];
        if (questions.Count == 0)
        {
            return "A quiz must have at least one question to be published.";
        }

        for (var index = 0; index < questions.Count; index++)
        {
            var questionMessage = ValidateQuestion(questions[index]);
            if (questionMessage is not null)
            {
                return $"Question {index + 1}: {questionMessage}";
            }
        }

        return null;
    }

    private string? ValidateQuestion(Question question)
    {
        var candidate = QuizAdministrationMapper.ToDto(question);
        var shapeMessage = _shapeValidator.Validate(candidate);
        if (shapeMessage is not null)
        {
            return shapeMessage;
        }

        var answersMessage = _answersValidator.ValidateAndNormalize(candidate, out var answers);
        if (answersMessage is not null)
        {
            return answersMessage;
        }

        var correctAnswerMessage = _correctAnswerValidator.Validate(answers, candidate.CorrectAnswer.Trim());
        if (correctAnswerMessage is not null)
        {
            return correctAnswerMessage;
        }

        if (candidate.TypeOfAnswer == TypeOfAnswer.TRUEORFALSE && answers.Length != 2)
        {
            return "A true or false question must have exactly two options.";
        }

        return null;
    }
}
