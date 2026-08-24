namespace GameSphere_backend.Services;

public interface ICorrectAnswerValidator
{
    string? Validate(string[] answers, string? correctAnswer);
}

public sealed class CorrectAnswerValidator : ICorrectAnswerValidator
{
    public string? Validate(string[] answers, string? correctAnswer)
    {
        return answers.Contains(correctAnswer, StringComparer.Ordinal)
            ? null
            : "The correct answer must be one of the question options.";
    }
}
