namespace GameSphere_backend.Models.FrontendModels
{
    public class GlobalRankingDto
    {
        public int Id { get; set; }

        public int? QuizzId { get; set; }

        public int? GameId { get; set; }

        public int UserId { get; set; }

        public float BestPontuation { get; set; }

        public int PositionOnRanking { get; set; }
    }
}
