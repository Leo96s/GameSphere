using GameSphere_backend.Models.FrontendModels;

namespace GameSphere_backend.ServicesResponses
{
    /// <summary>
    /// Represents the response returned after successful user authentication.
    /// </summary>
    /// <remarks>
    /// This class contains both the JWT token for authorization and the user's profile information.
    /// It is returned by authentication endpoints to provide clients with everything needed
    /// to maintain an authenticated session.
    /// </remarks>
    public class LoginResponse
    {
        /// <summary>
        /// Gets or sets the JWT token generated for authentication.
        /// </summary>
        /// <value>
        /// The JSON Web Token that should be included in subsequent requests for authorization.
        /// </value>
        /// <remarks>
        /// O token JWT gerado para autenticação.
        /// </remarks>
        public string token { get; set; }

        /// <summary>
        /// Gets or sets the details of the authenticated user.
        /// </summary>
        /// <value>
        /// Contains the user's profile information including ID, name, email, and other relevant details.
        /// </value>
        /// <remarks>
        /// Os detalhes do utilizador autenticado.
        /// </remarks>
        public UserDto user { get; set; }
    }

}
