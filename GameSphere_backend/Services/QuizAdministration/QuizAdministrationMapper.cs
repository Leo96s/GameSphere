using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.Services.QuizAdministration;

internal static class QuizAdministrationMapper
{
    public static Question ToEntity(AdminQuestionUpsertDto source, int quizId) => new()
    {
        Description = source.Description,
        TypeOfAnswer = source.TypeOfAnswer,
        Answers = source.Answers,
        CorrectAnswer = source.CorrectAnswer,
        QuizzId = quizId
    };

    public static AdminQuizUpsertDto ToDto(Quizz source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Difficulty = source.Difficulty,
        NumberOfQuests = source.NumberOfQuests,
        IsPublished = source.IsPublished,
        Questions = source.Questions?
            .OrderBy(question => question.Id)
            .Select(ToDto)
            .ToList() ?? []
    };

    public static AdminQuestionUpsertDto ToDto(Question source) => new()
    {
        Id = source.Id,
        Description = source.Description,
        TypeOfAnswer = source.TypeOfAnswer,
        Answers = source.Answers,
        CorrectAnswer = source.CorrectAnswer,
        QuizzId = source.QuizzId
    };
}
