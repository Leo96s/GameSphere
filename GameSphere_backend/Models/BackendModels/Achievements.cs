using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class Achievements
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'nameOfAchievement' is required.")]
        public string NameOfAchievement { get; set; }

        [ForeignKey("User")]
        public int UserId;

        public virtual User User { get; set; }

        [Required(ErrorMessage = "The field 'date' is required.")]
        public DateTime Date { get; set; }

        public virtual ICollection<Requirement> RequirementsForUnlock { get; set; }



    }
}
