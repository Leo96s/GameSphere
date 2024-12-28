using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Utils;

namespace GameSphere_backend.Mappers
{
    public class GameMapper
    {
        public static GameDto? GameToDto(Game game)
        {
            if (game == null) return null;

            return new GameDto
            {
                Id = game.Id,
                Name = game.Name,
                TypeOfGame = game.TypoOfGame,
                Description = game.Description,
            };
        }

        public static Game? GameToModel(GameDto game)
        {
            if(game == null) return null;

            var gameModel = new Game
            {
                Id = game.Id,
                Name = game.Name,
                TypoOfGame = game.TypeOfGame,
                Description = game.Description,
            };

            ConversionValidate.ValidateModel(gameModel);

            return gameModel;
            
        }

    }
}
