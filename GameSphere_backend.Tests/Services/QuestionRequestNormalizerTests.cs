#nullable enable

using System.Collections.Generic;
using System.Linq;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class QuestionRequestNormalizerTests
{
    [Theory]
    [MemberData(nameof(InvalidQuizRequests))]
    public void Quiz_request_normalizer_rejects_invalid_boundaries(
        AdminQuizUpsertDto request,
        string expectedMessage)
    {
        var normalizer = CreateQuizNormalizer();

        var result = normalizer.TryNormalize(request, out var normalized, out var message);

        Assert.False(result);
        Assert.Equal(expectedMessage, message);
        Assert.Equal(string.Empty, normalized.Title);
    }

    [Theory]
    [MemberData(nameof(InvalidQuestionRequests))]
    public void Question_request_normalizer_rejects_invalid_boundaries(
        AdminQuestionUpsertDto request,
        string expectedMessage)
    {
        var normalizer = CreateQuestionNormalizer();

        var result = normalizer.TryNormalize(request, out var normalized, out var message);

        Assert.False(result);
        Assert.Equal(expectedMessage, message);
        Assert.Equal(string.Empty, normalized.Description);
    }

    [Fact]
    public void Question_request_normalizer_trims_description_options_and_correct_answer()
    {
        var normalizer = CreateQuestionNormalizer();

        var result = normalizer.TryNormalize(new AdminQuestionUpsertDto
        {
            Description = "  Capital of Portugal?  ",
            TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
            Answers = ["  Lisbon  ", "Porto"],
            CorrectAnswer = "  Lisbon  "
        }, out var normalized, out var message);

        Assert.True(result);
        Assert.Equal(string.Empty, message);
        Assert.Equal("Capital of Portugal?", normalized.Description);
        Assert.Equal(["Lisbon", "Porto"], normalized.Answers);
        Assert.Equal("Lisbon", normalized.CorrectAnswer);
    }

    public static IEnumerable<object[]> InvalidQuizRequests()
    {
        yield return
        [
            ValidQuiz().WithTitle(" "),
            "A quiz title is required."
        ];
        yield return
        [
            ValidQuiz().WithTitle(new string('A', 101)),
            "The quiz contains an invalid title or difficulty."
        ];
        yield return
        [
            ValidQuiz().WithDifficulty((Difficulty)99),
            "The quiz contains an invalid title or difficulty."
        ];
        yield return
        [
            ValidQuiz().WithQuestions(Enumerable.Range(0, 101).Select(_ => ValidQuestion()).ToList()),
            "A quiz cannot contain more than 100 questions."
        ];
    }

    public static IEnumerable<object[]> InvalidQuestionRequests()
    {
        yield return
        [
            ValidQuestion().WithDescription(" "),
            "A question, supported answer type, options and correct answer are required."
        ];
        yield return
        [
            ValidQuestion().WithAnswers(["Only one"]),
            "A question, supported answer type, options and correct answer are required."
        ];
        yield return
        [
            ValidQuestion().WithAnswers(["Option A", "  Option A  "]),
            "Each question option must be unique and non-empty."
        ];
        yield return
        [
            ValidQuestion().WithAnswers(["Option A", " "]),
            "Each question option must be unique and non-empty."
        ];
        yield return
        [
            ValidQuestion().WithAnswers(["Option A", new string('B', 501)]),
            "Each question option must be unique and non-empty."
        ];
        yield return
        [
            ValidQuestion().WithCorrectAnswer("Option C"),
            "The correct answer must be one of the question options."
        ];
        yield return
        [
            ValidQuestion().WithTypeOfAnswer((TypeOfAnswer)99),
            "A question, supported answer type, options and correct answer are required."
        ];
    }

    private static QuizRequestNormalizer CreateQuizNormalizer() => new(CreateQuestionNormalizer());

    private static QuestionRequestNormalizer CreateQuestionNormalizer() => new(
        new QuestionShapeValidator(),
        new QuestionAnswersValidator(),
        new CorrectAnswerValidator());

    private static AdminQuizUpsertDto ValidQuiz() => new()
    {
        Title = "Valid quiz",
        Difficulty = Difficulty.EASY,
        IsPublished = false
    };

    private static AdminQuestionUpsertDto ValidQuestion() => new()
    {
        Description = "Valid question?",
        TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
        Answers = ["Option A", "Option B"],
        CorrectAnswer = "Option A"
    };
}

file static class AdminQuizUpsertDtoTestExtensions
{
    public static AdminQuizUpsertDto WithTitle(this AdminQuizUpsertDto source, string title)
    {
        source.Title = title;
        return source;
    }

    public static AdminQuizUpsertDto WithDifficulty(this AdminQuizUpsertDto source, Difficulty difficulty)
    {
        source.Difficulty = difficulty;
        return source;
    }

    public static AdminQuizUpsertDto WithQuestions(
        this AdminQuizUpsertDto source,
        List<AdminQuestionUpsertDto> questions)
    {
        source.Questions = questions;
        return source;
    }
}

file static class AdminQuestionUpsertDtoTestExtensions
{
    public static AdminQuestionUpsertDto WithDescription(this AdminQuestionUpsertDto source, string description)
    {
        source.Description = description;
        return source;
    }

    public static AdminQuestionUpsertDto WithTypeOfAnswer(this AdminQuestionUpsertDto source, TypeOfAnswer typeOfAnswer)
    {
        source.TypeOfAnswer = typeOfAnswer;
        return source;
    }

    public static AdminQuestionUpsertDto WithAnswers(this AdminQuestionUpsertDto source, string[] answers)
    {
        source.Answers = answers;
        return source;
    }

    public static AdminQuestionUpsertDto WithCorrectAnswer(this AdminQuestionUpsertDto source, string correctAnswer)
    {
        source.CorrectAnswer = correctAnswer;
        return source;
    }
}
