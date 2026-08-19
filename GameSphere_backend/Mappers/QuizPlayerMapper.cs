using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Mappers;

public static class QuizPlayerMapper
{
    public static QuizCatalogItemDto ToCatalogItem(Quizz quiz) => new()
    {
        Id = quiz.Id,
        Title = quiz.Title,
        Difficulty = quiz.Difficulty,
        NumberOfQuests = quiz.NumberOfQuests,
    };

    public static QuizPlayDto ToPlayQuiz(Quizz quiz) => new()
    {
        Id = quiz.Id,
        Title = quiz.Title,
        Difficulty = quiz.Difficulty,
        Questions = (quiz.Questions ?? []).Select(ToQuestion).ToArray(),
    };

    private static QuizQuestionDto ToQuestion(Question question) => new()
    {
        Id = question.Id,
        Description = question.Description,
        TypeOfAnswer = question.TypeOfAnswer,
        Answers = question.Answers,
    };
}
