using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class RequirementMapper
    {
        public static RequirementDto? RequirementToDto(Requirement requirement)
        {
            if (requirement == null) return null;

            return new RequirementDto
            {
                Id = requirement.Id,
                Description = requirement.Description,
                AchievementId = requirement.AchievementId,
            };
        }
        public static Requirement? RequirementToModel(RequirementDto requirement)
        {
            if (requirement == null) return null;

            var requirementModel = new Requirement
            {
                Id = requirement.Id,
                Description = requirement.Description,
                AchievementId = requirement.AchievementId,
            };

            ConversionValidate.ValidateModel(requirementModel);

            return requirementModel;
        }
    }
}
