using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class AchievementsMapper
    {
        public static AchievementsDto? AchievementsToDto(Achievements achievements)
        {
            if (achievements == null) return null;

            var registrationDate = achievements.Date == default ? DateTime.Now : achievements.Date;

            return new AchievementsDto
            {
                Id = achievements.Id,
                NameOfAchievment = achievements.NameOfAchievement,
                UserId = achievements.UserId,
                Date = registrationDate,
            };
        }

        public static Achievements? AchievementsToModel(AchievementsDto achievements)
        {
            if(achievements == null) return null;   

            if (achievements.Date == default) achievements.Date = DateTime.Now;

            var achievementsModel = new Achievements
            {
                Id = achievements.Id,
                NameOfAchievement = achievements.NameOfAchievment,
                UserId = achievements.UserId,
                Date = achievements.Date,
            };

            ConversionValidate.ValidateModel(achievementsModel);

            return achievementsModel;
        }
    }
}
