using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    public class QuestionDto
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public TypeOfAnswer TypeOfAnswer { get; set; }

        public string[] Answers { get; set; }

        public string CorrectAnswer { get; set; }

        public int QuizzId { get; set; }


    }
}
