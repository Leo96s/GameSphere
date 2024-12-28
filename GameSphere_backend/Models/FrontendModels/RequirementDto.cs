using GameSphere_backend.Models.BackendModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels
{
    public class RequirementDto
    {
        public int Id { get; set; }
        public string Description { get; set; }

        public int AchievementId { get; set; }


    }
}
