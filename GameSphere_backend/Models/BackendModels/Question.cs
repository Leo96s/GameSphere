using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameSphere_backend.Models.BackendModels
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The field 'description' is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "The field 'typeOfAnswer' is required.")]
        public TypeOfAnswer TypeOfAnswer { get; set; }

        [Required(ErrorMessage = "The field 'answers' is required.")]
        public string[] Answers { get; set; }

        [Required(ErrorMessage = "The field 'coreectAnswer' is required.")]
        public string CorrectAnswer { get; set; }


        [ForeignKey("Quizz")]
        public int QuizzId { get; set; }

        public virtual Quizz Quizz { get; set; }
    }
}
