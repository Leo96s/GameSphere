using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    public class QuizzDto
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public Difficulty Difficulty { get; set; }

        public int NumberOfQuests { get; set; }

        public DateTime RegistrationDate { get; set; }

        public int UserId { get; set; }


    }
}
