using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.BackendModels
{
    public class Requirement
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'description' is required.")]
        public string Description { get; set; }

        [ForeignKey("Achievement")]
        public int AchievementId { get; set; }

        public virtual Achievements Achievement { get; set; }
    }
}
