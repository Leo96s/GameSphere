using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    public class QuizzDto
    {
        public string Title { get; set; }

        public Difficulty Difficulty { get; set; }

        public int NumberOfQuests { get; set; }


    }
}
