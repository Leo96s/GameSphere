using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class QuestionMapper
    {
        public static QuestionDto? QuestionToDto(Question question)
        {
            if (question == null) return null;

            return new QuestionDto
            {
                Id = question.Id,
                Description = question.Description,
                TypeOfAnswer = question.TypeOfAnswer,
                Answers = question.Answers,
                CorrectAnswer = question.CorrectAnswer,
                QuizzId = question.QuizzId,
            };
        }

        public static Question? QuestionToModel(QuestionDto question)
        {
            if (question == null) return null;

            var questionModel = new Question
            {
                Id = question.Id,
                Description = question.Description,
                TypeOfAnswer = question.TypeOfAnswer,
                Answers = question.Answers,
                CorrectAnswer = question.CorrectAnswer,
                QuizzId = question.QuizzId,
            };

            ConversionValidate.ValidateModel(questionModel);

            return questionModel;
        }
    }
}
