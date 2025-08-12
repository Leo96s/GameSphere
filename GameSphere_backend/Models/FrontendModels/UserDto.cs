using GameSphere_backend.Enums;

namespace GameSphere_backend.Models.FrontendModels
{
    /// <summary>
    /// Represents a User Data Transfer Object (DTO) for frontend communication.
    /// </summary>
    /// <remarks>
    /// This class is used to transfer user data between backend and frontend,
    /// containing all necessary user information while excluding sensitive data
    /// that shouldn't be exposed to the client.
    /// </remarks>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the external provider's unique identifier for the user.
        /// </summary>
        /// <remarks>
        /// Used for social login integrations (Google, Facebook, etc.).
        /// </remarks>
        public string UID { get; set; }

        /// <summary>
        /// Gets or sets the hashed password for the user.
        /// </summary>
        /// <remarks>
        /// Should always be hashed using a secure algorithm like BCrypt.
        /// </remarks>
        public string HashedPassword { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        /// <remarks>
        /// Used as the primary contact method and for authentication.
        /// </remarks>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's gender identity.
        /// </summary>
        public Gender Gender { get; set; }

        /// <summary>
        /// Gets or sets the URL or path to the user's profile image.
        /// </summary>
        /// <remarks>
        /// Optional field - can be null if user hasn't uploaded an image.
        /// </remarks>
        public string? Image { get; set; }

        /// <summary>
        /// Gets or sets the user's current level in the application.
        /// </summary>
        /// <remarks>
        /// Represents progression or achievement level in the system.
        /// </remarks>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the total points accumulated by the user.
        /// </summary>
        public long TotalPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool isActive { get; set; }

        /// <summary>
        /// Gets or sets the authentication token for the user.
        /// </summary>
        /// <remarks>
        /// Used for maintaining authenticated sessions. Can be null when not logged in.
        /// </remarks>
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the expiration date/time for the authentication token.
        /// </summary>
        public DateTime? TokenExpDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the user registered in the system.
        /// </summary>
        public DateTime RegistrationDate { get; set; }
    }
}
