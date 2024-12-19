using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models
{
    public class Quizz
    {
        [Key]
        public int id {  get; set; }

        [Required(ErrorMessage = "The field 'title' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'title' has to be less than 100 characters.")]
        public string title { get; set; }

        public Difficulty difficulty { get; set; }

        public int numberOfQuests { get; set; }

        [Required(ErrorMessage = "The field 'RegistrationDate' is required.")]
        public DateTime registrationDate { get; set; }

        [ForeignKey("User")]
        public int userId { get; set; }
        public virtual User user { get; set; }

        public ICollection<Quizz> Quizzs { get; set; }
    }
}
