using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class Quizz
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'title' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'title' has to be less than 100 characters.")]
        public string Title { get; set; }

        public Difficulty Difficulty { get; set; }

        public int NumberOfQuests { get; set; }

        [Required(ErrorMessage = "The field 'RegistrationDate' is required.")]
        public DateTime RegistrationDate { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public ICollection<Question> Questions { get; set; }

        public ICollection<Score> Scores { get; set; }
    }
}
