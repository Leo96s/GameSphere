using GameSphere_backend.Models.BackendModels;

namespace GameSphere_backend.Interfaces
{
    /// <summary>
    /// Defines the contract for authentication services in the application.
    /// </summary>
    /// <remarks>
    /// This interface provides the abstraction for token generation functionality,
    /// allowing for different implementations while maintaining a consistent API.
    /// </remarks>
    public interface IAuthService
    {
        /// <summary>
        /// Generates a JSON Web Token (JWT) for user authentication.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <returns>
        /// A signed JWT string containing the user's claims.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when either userId or email is null or empty.
        /// </exception>
        /// <remarks>
        /// The generated token will include standard claims for:
        /// - User identification (sub claim)
        /// - Email address
        /// - Token expiration
        /// - Unique token identifier (jti)
        /// 
        /// Implementations should:
        /// 1. Validate input parameters
        /// 2. Use secure signing algorithms (e.g., HMAC-SHA256)
        /// 3. Set appropriate expiration time
        /// 4. Include necessary claims for authorization
        /// </remarks>
        string GenerateToken(string userId, string email);
    }
}
