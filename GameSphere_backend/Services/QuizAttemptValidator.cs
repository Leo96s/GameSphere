using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services;

internal sealed record ValidatedQuizAttempt(
    IReadOnlyList<Question> Questions,
    IReadOnlyDictionary<int, QuizAnswerRequest> AnswersByQuestion);

public sealed class QuizAttemptValidator
{
    internal ServiceResponse<ValidatedQuizAttempt> Validate(Quizz? quiz, QuizAttemptRequest request)
    {
        if (quiz is null)
        {
            return Failure("NotFound", "Quiz not found.");
        }

        var questions = quiz.Questions?.ToArray() ?? [];

        if (questions.Length == 0)
        {
            return Failure("BadRequest", "Quiz has no questions.");
        }

        if (request.Answers.Count != questions.Length)
        {
            return Failure("BadRequest", "An answer is required for every quiz question.");
        }

        var answersByQuestion = CollectAnswersByQuestion(request.Answers);
        if (answersByQuestion is null)
        {
            return Failure("BadRequest", "Each quiz question must have one valid answer.");
        }

        if (!MatchesQuizQuestions(questions, answersByQuestion))
        {
            return Failure("BadRequest", "The submitted answers do not match this quiz.");
        }

        if (!AllSelectedAnswersAreOptions(questions, answersByQuestion))
        {
            return Failure("BadRequest", "Each selected answer must be one of the question options.");
        }

        return new ServiceResponse<ValidatedQuizAttempt>
        {
            Success = true,
            Type = "Ok",
            Data = new ValidatedQuizAttempt(questions, answersByQuestion)
        };
    }

    private static Dictionary<int, QuizAnswerRequest>? CollectAnswersByQuestion(
        IReadOnlyList<QuizAnswerRequest> submittedAnswers)
    {
        var answersByQuestion = new Dictionary<int, QuizAnswerRequest>();
        foreach (var answer in submittedAnswers)
        {
            if (string.IsNullOrWhiteSpace(answer.SelectedAnswer) ||
                !answersByQuestion.TryAdd(answer.QuestionId, answer))
            {
                return null;
            }
        }

        return answersByQuestion;
    }

    private static bool MatchesQuizQuestions(
        IReadOnlyList<Question> questions,
        IReadOnlyDictionary<int, QuizAnswerRequest> answersByQuestion)
    {
        var questionIds = questions.Select(question => question.Id).ToHashSet();

        return answersByQuestion.Keys.ToHashSet().SetEquals(questionIds);
    }

    private static bool AllSelectedAnswersAreOptions(
        IReadOnlyList<Question> questions,
        IReadOnlyDictionary<int, QuizAnswerRequest> answersByQuestion)
    {
        return questions.All(question =>
            question.Answers.Contains(
                answersByQuestion[question.Id].SelectedAnswer,
                StringComparer.Ordinal));
    }

    private static ServiceResponse<ValidatedQuizAttempt> Failure(string type, string message) => new()
    {
        Success = false,
        Type = type,
        Message = message
    };
}
