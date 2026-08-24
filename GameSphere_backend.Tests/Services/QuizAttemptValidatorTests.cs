#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Services;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class QuizAttemptValidatorTests
{
    [Fact]
    public void Validator_rejects_missing_quiz_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(null, Request());

        Assert.False(result.Success);
        Assert.Equal("NotFound", result.Type);
        Assert.Equal("Quiz not found.", result.Message);
    }

    [Fact]
    public void Validator_rejects_quiz_without_questions_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(Quiz([]), Request());

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Type);
        Assert.Equal("Quiz has no questions.", result.Message);
    }

    [Fact]
    public void Validator_rejects_missing_answer_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(Quiz(Questions()), Request(
        [
            Answer(1, "Alpha")
        ]));

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Type);
        Assert.Equal("An answer is required for every quiz question.", result.Message);
    }

    [Fact]
    public void Validator_rejects_duplicate_answer_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(Quiz(Questions()), Request(
        [
            Answer(1, "Alpha"),
            Answer(1, "Alpha")
        ]));

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Type);
        Assert.Equal("Each quiz question must have one valid answer.", result.Message);
    }

    [Fact]
    public void Validator_rejects_unknown_question_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(Quiz(Questions()), Request(
        [
            Answer(1, "Alpha"),
            Answer(987654, "Unexpected")
        ]));

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Type);
        Assert.Equal("The submitted answers do not match this quiz.", result.Message);
    }

    [Fact]
    public void Validator_rejects_invalid_option_with_existing_message()
    {
        var result = new QuizAttemptValidator().Validate(Quiz(Questions()), Request(
        [
            Answer(1, "Invalid option"),
            Answer(2, "Bravo")
        ]));

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Type);
        Assert.Equal("Each selected answer must be one of the question options.", result.Message);
    }

    [Fact]
    public void Validator_accepts_complete_attempt_by_question()
    {
        var result = new QuizAttemptValidator().Validate(Quiz(Questions()), Request());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal([1, 2], result.Data.Questions.Select(question => question.Id));
        Assert.Equal("Alpha", result.Data.AnswersByQuestion[1].SelectedAnswer);
        Assert.Equal("Bravo", result.Data.AnswersByQuestion[2].SelectedAnswer);
    }

    private static QuizAttemptRequest Request(IReadOnlyList<QuizAnswerRequest>? answers = null) => new()
    {
        Answers = answers ??
        [
            Answer(1, "Alpha"),
            Answer(2, "Bravo")
        ]
    };

    private static QuizAnswerRequest Answer(int questionId, string selectedAnswer) => new()
    {
        QuestionId = questionId,
        SelectedAnswer = selectedAnswer
    };

    private static Quizz Quiz(ICollection<Question>? questions) => new()
    {
        Id = 10,
        Title = "Attempt validation quiz",
        Difficulty = Difficulty.EASY,
        NumberOfQuests = questions?.Count ?? 0,
        IsPublished = true,
        RegistrationDate = DateTime.UtcNow,
        UserId = 20,
        Questions = questions
    };

    private static List<Question> Questions() =>
    [
        Question(1, "First question", ["Alpha", "Wrong first answer"], "Alpha"),
        Question(2, "Second question", ["Bravo", "Wrong second answer"], "Bravo")
    ];

    private static Question Question(
        int id,
        string description,
        string[] answers,
        string correctAnswer) => new()
        {
            Id = id,
            Description = description,
            TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
            Answers = answers,
            CorrectAnswer = correctAnswer,
            QuizzId = 10
        };
}
