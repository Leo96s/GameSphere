using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.ServicesResponses
{
    public class LoginResponse
    
    {
            /// <summary>
            /// O token JWT gerado para autenticação.
            /// </summary>
            public string token { get; set; }

            /// <summary>
            /// Os detalhes do utilizador autenticado.
            /// </summary>
            public UserDto user { get; set; }
    }
 
}
