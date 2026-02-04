using GameSphere_backend.Models.BackendModels;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using GameSphere_backend.Interfaces;

namespace GameSphere_backend.Services
{
    /// <summary>
    /// Service responsible for authentication-related operations, primarily JWT token generation.
    /// </summary>
    /// <remarks>
    /// This service implements the IAuthService interface and provides functionality
    /// for creating and validating JWT tokens used for authentication in the GameSphere system.
    /// </remarks>
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the AuthService class.
        /// </summary>
        /// <param name="config">The application configuration containing JWT settings.</param>
        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates a JWT token for user authentication with a specified expiration time.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <returns>
        /// A signed JWT token string containing the user's claims.
        /// </returns>
        /// <remarks>
        /// The token includes the following standard claims:
        /// - Subject (sub): The user's ID
        /// - Email: The user's email address
        /// - JWT ID (jti): A unique identifier for the token
        /// 
        /// Token configuration is read from appsettings.json under the JwtSettings section:
        /// - SecretKey: The signing key for the token
        /// - Issuer: The token issuer
        /// - Audience: The intended token audience
        /// - ExpirationMinutes: Token validity period in minutes
        /// </remarks>
        public string GenerateToken(string userId, string email)
        {
            // Retrieve JWT configuration from app settings
            var secretKey = _config["JwtSettings:SecretKey"];
            var issuer = _config["JwtSettings:Issuer"];
            var audience = _config["JwtSettings:Audience"];
            var expirationStr = _config["JwtSettings:ExpirationMinutes"]
                ?? throw new Exception("JWT ExpirationMinutes is missing in config");

            var expiration = int.Parse(expirationStr);

            // Create signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create token claims
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Identificador único
        };

            // Generate and return the token
            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiration),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
