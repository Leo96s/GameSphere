using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class QuizzMapper
    {
        public static QuizzDto? QuizzToDto(Quizz quizz)
        {
            if (quizz == null) return null;

            var registrationDate = quizz.RegistrationDate == default ? DateTime.Now : quizz.RegistrationDate;

            return new QuizzDto
            {
                Id = quizz.Id,
                Title = quizz.Title,
                Difficulty = quizz.Difficulty,
                NumberOfQuests = quizz.NumberOfQuests,
                RegistrationDate = registrationDate,
                UserId = quizz.UserId,
            };
        }

        public static Quizz? QuizzToModel(QuizzDto quizz)
        {
            if (quizz == null) return null;

            if (quizz.RegistrationDate == default) quizz.RegistrationDate = DateTime.Now;

            var quizzModel = new Quizz
            {
                Id = quizz.Id,
                Title = quizz.Title,
                Difficulty = quizz.Difficulty,
                NumberOfQuests = quizz.NumberOfQuests,
                RegistrationDate = quizz.RegistrationDate,
                UserId = quizz.UserId,
            };

            ConversionValidate.ValidateModel(quizzModel);

            return quizzModel;

        }

    }
}
