#nullable enable

using System;
using System.Collections.Generic;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Services;
using Xunit;

namespace GameSphere_backend.Tests.Services;

public sealed class QuizPublicationValidatorTests
{
    [Fact]
    public void Valid_quiz_passes_publication_validation()
    {
        var quiz = Quiz(Multiple(1), TrueOrFalse(2));

        Assert.Null(CreateValidator().Validate(quiz));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Quiz_without_title_cannot_be_published(string title)
    {
        var quiz = Quiz(Multiple(1));
        quiz.Title = title;

        Assert.Equal("A quiz title is required.", CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Quiz_with_a_title_that_is_too_long_cannot_be_published()
    {
        var quiz = Quiz(Multiple(1));
        quiz.Title = new string('A', 101);

        Assert.Equal("The quiz title cannot exceed 100 characters.", CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Quiz_with_an_undefined_difficulty_cannot_be_published()
    {
        var quiz = Quiz(Multiple(1));
        quiz.Difficulty = (Difficulty)99;

        Assert.Equal("The quiz difficulty is invalid.", CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Quiz_with_an_undefined_category_cannot_be_published()
    {
        var quiz = Quiz(Multiple(1));
        quiz.Category = (QuizCategory)99;

        Assert.Equal("The quiz category is invalid.", CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Quiz_without_questions_cannot_be_published()
    {
        var empty = Quiz();
        var nullQuestions = Quiz();
        nullQuestions.Questions = null;

        Assert.Equal("A quiz must have at least one question to be published.", CreateValidator().Validate(empty));
        Assert.Equal("A quiz must have at least one question to be published.", CreateValidator().Validate(nullQuestions));
    }

    [Fact]
    public void Shape_errors_report_the_question_number()
    {
        var invalid = Multiple(2);
        invalid.Answers = ["Only one"];

        Assert.Equal(
            "Question 2: A question, supported answer type, options and correct answer are required.",
            CreateValidator().Validate(Quiz(Multiple(1), invalid)));
    }

    [Fact]
    public void Unsupported_answer_type_is_rejected_with_the_question_number()
    {
        var invalid = Multiple(1);
        invalid.TypeOfAnswer = (TypeOfAnswer)99;

        Assert.Equal(
            "Question 1: A question, supported answer type, options and correct answer are required.",
            CreateValidator().Validate(Quiz(invalid)));
    }

    [Fact]
    public void Duplicate_options_are_rejected_with_the_question_number()
    {
        var invalid = Multiple(1);
        invalid.Answers = ["Option A", "  Option A  "];

        Assert.Equal(
            "Question 1: Each question option must be unique and non-empty.",
            CreateValidator().Validate(Quiz(invalid)));
    }

    [Fact]
    public void Correct_answer_outside_the_options_is_rejected_with_the_question_number()
    {
        var invalid = Multiple(3);
        invalid.CorrectAnswer = "Option C";

        Assert.Equal(
            "Question 3: The correct answer must be one of the question options.",
            CreateValidator().Validate(Quiz(Multiple(1), Multiple(2), invalid)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void True_or_false_question_must_have_exactly_two_options(int optionCount)
    {
        var invalid = TrueOrFalse(1);
        invalid.Answers = optionCount == 3 ? ["True", "False", "Maybe"] : ["True", "False", "Maybe", "Never"];

        Assert.Equal(
            "Question 1: A true or false question must have exactly two options.",
            CreateValidator().Validate(Quiz(invalid)));
    }

    [Fact]
    public void Question_number_is_the_position_not_the_database_identifier()
    {
        var invalid = Multiple(57);
        invalid.CorrectAnswer = "Option C";

        var message = CreateValidator().Validate(Quiz(Multiple(10), invalid));

        Assert.NotNull(message);
        Assert.StartsWith("Question 2: ", message, StringComparison.Ordinal);
        Assert.DoesNotContain("57", message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_first_failing_question_is_reported()
    {
        var second = Multiple(2);
        second.CorrectAnswer = "Option C";
        var third = Multiple(3);
        third.Answers = ["Only one"];

        Assert.StartsWith("Question 2: ", CreateValidator().Validate(Quiz(Multiple(1), second, third)), StringComparison.Ordinal);
    }

    [Fact]
    public void Title_with_forbidden_characters_is_rejected_without_prefix()
    {
        var quiz = Quiz(Multiple(1));
        quiz.Title = "Bad\u202Etitle";

        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Title_with_a_line_break_is_rejected()
    {
        var quiz = Quiz(Multiple(1));
        quiz.Title = "Line\nbreak";

        Assert.Equal(EditorialTextValidator.UnsupportedCharactersMessage, CreateValidator().Validate(quiz));
    }

    [Fact]
    public void Description_with_forbidden_characters_is_rejected_with_the_question_number()
    {
        var invalid = Multiple(2);
        invalid.Description = "Bad\u202E?";

        Assert.Equal(
            "Question 2: " + EditorialTextValidator.UnsupportedCharactersMessage,
            CreateValidator().Validate(Quiz(Multiple(1), invalid)));
    }

    [Fact]
    public void Description_with_line_breaks_is_accepted()
    {
        var question = Multiple(1);
        question.Description = "Line one\nLine two";

        Assert.Null(CreateValidator().Validate(Quiz(question)));
    }

    [Fact]
    public void Option_with_forbidden_characters_or_line_breaks_is_rejected_with_the_question_number()
    {
        var forbidden = Multiple(1);
        forbidden.Answers = ["Option A", "Option\u200BB"];
        var lineBreak = Multiple(1);
        lineBreak.Answers = ["Option A", "Option\nB"];

        Assert.Equal(
            "Question 1: " + EditorialTextValidator.UnsupportedCharactersMessage,
            CreateValidator().Validate(Quiz(forbidden)));
        Assert.Equal(
            "Question 1: " + EditorialTextValidator.UnsupportedCharactersMessage,
            CreateValidator().Validate(Quiz(lineBreak)));
    }

    [Fact]
    public void Correct_answer_with_forbidden_characters_is_rejected_with_the_question_number()
    {
        var invalid = Multiple(1);
        invalid.CorrectAnswer = "Option\u202EA";

        Assert.Equal(
            "Question 1: " + EditorialTextValidator.UnsupportedCharactersMessage,
            CreateValidator().Validate(Quiz(invalid)));
    }

    private static QuizPublicationValidator CreateValidator() => new(
        new QuestionShapeValidator(),
        new QuestionAnswersValidator(),
        new CorrectAnswerValidator(),
        new EditorialTextValidator());

    private static Quizz Quiz(params Question[] questions) => new()
    {
        Id = 1,
        Title = "Valid quiz",
        Difficulty = Difficulty.EASY,
        NumberOfQuests = questions.Length,
        RegistrationDate = DateTime.UtcNow,
        UserId = 1,
        Status = QuizStatus.Draft,
        Questions = new List<Question>(questions)
    };

    private static Question Multiple(int id) => new()
    {
        Id = id,
        Description = $"Question {id}?",
        TypeOfAnswer = TypeOfAnswer.MULTIPLEOPTION,
        Answers = ["Option A", "Option B"],
        CorrectAnswer = "Option A",
        QuizzId = 1
    };

    private static Question TrueOrFalse(int id) => new()
    {
        Id = id,
        Description = $"Statement {id}",
        TypeOfAnswer = TypeOfAnswer.TRUEORFALSE,
        Answers = ["True", "False"],
        CorrectAnswer = "True",
        QuizzId = 1
    };
}
