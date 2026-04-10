using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.BackendModels
{
    public class Game
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'name' is required.")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "The field 'TypeOfGame' is required.")]
        public required TypoOfGame TypoOfGame { get; set; }

        [Required(ErrorMessage = "The field 'description' is required.")]
        public required string Description { get; set; }

        public ICollection<Score>? Scores { get; set; }
    }
}
