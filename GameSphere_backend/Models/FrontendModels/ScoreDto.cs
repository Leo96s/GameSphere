namespace GameSphere_backend.Models.FrontendModels
{
    public class ScoreDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? QuizzId { get; set; }

        public int? GameId { get; set; }

        public float Points {  get; set; }

        public DateTime Date { get; set; }

    }
}
