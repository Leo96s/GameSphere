using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    public class GameDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public TypoOfGame TypeOfGame { get; set; }

        public string Description { get; set; }


    }
}
