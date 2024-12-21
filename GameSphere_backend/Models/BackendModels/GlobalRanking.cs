using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class GlobalRanking
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Quizz")]
        public int? QuizzId { get; set; }

        public virtual Quizz Quizz { get; set; }

        [ForeignKey("Game")]
        public int? GameId { get; set; }

        public virtual Game Game { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public virtual User User { get; set; }

        [Required(ErrorMessage = "The field 'bestPontuation' is required.")]
        public float BestPontuation { get; set; }

        [Required(ErrorMessage = "The field 'position' is required.")]
        public int PositionOnRanking { get; set; }

    }
}
