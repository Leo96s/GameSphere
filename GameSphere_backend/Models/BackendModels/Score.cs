using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class Score
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public virtual User User { get; set; }

        [ForeignKey("Quizz")]
        public int? QuizzId { get; set; }

        public virtual Quizz Quizz { get; set; }

        [ForeignKey("Game")]
        public int? GameId { get; set; }

        public virtual Game Game { get; set; }

        [Required(ErrorMessage = "The field 'points' is required.")]
        public float Points { get; set; }

        [Required(ErrorMessage = "The field 'date' is required.")]
        public DateTime Date { get; set; }

    }
}
