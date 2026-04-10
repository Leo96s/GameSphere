using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class Achievements
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'nameOfAchievement' is required.")]
        public required string NameOfAchievement { get; set; }

        [ForeignKey("User")]
        public required int UserId;

        public virtual User? User { get; set; }

        [Required(ErrorMessage = "The field 'date' is required.")]
        public required DateTime Date { get; set; }

        public virtual ICollection<Requirement>? RequirementsForUnlock { get; set; }



    }
}
