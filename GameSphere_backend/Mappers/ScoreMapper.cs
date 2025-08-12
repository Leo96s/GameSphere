using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class ScoreMapper
    {
        public static ScoreDto? ScoreToDto(Score score)
        {
            if(score == null) return null;

            var registrateDate = score.Date == default ? DateTime.Now : score.Date;

            return new ScoreDto
            {
                Id = score.Id,
                UserId = score.UserId,
                QuizzId = score.QuizzId,
                GameId = score.GameId,
                Points = score.Points,
                Date = registrateDate,
            };
        }

        public static Score? ScoreToModel(ScoreDto score)
        {
            if(score == null) return null;

            if(score.Date == default) score.Date = DateTime.Now;

            var scoreModel = new Score
            {
                Id = score.Id,
                UserId = score.UserId,
                QuizzId = score.QuizzId,
                GameId = score.GameId,
                Points = score.Points,
                Date = score.Date,
            };

            ConversionValidate.ValidateModel(scoreModel);

            return scoreModel;
        }
    }
}
