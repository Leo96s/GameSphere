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
        /// Initializes a new instance of the <see cref="UserDto"/> class for request deserialization.
        /// </summary>
        public UserDto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDto"/> class for a server response.
        /// </summary>
        /// <param name="role">The role persisted for the user.</param>
        public UserDto(UserRole role)
        {
            Role = role;
        }

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
        public string? UID { get; set; }

        /// <summary>
        /// Gets or sets the hashed password for the user.
        /// </summary>
        /// <remarks>
        /// Should always be hashed using a secure algorithm like BCrypt.
        /// </remarks>
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? HashedPassword { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First name is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public required string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        /// <remarks>
        /// Used as the primary contact method and for authentication.
        /// </remarks>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email is required.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email has an invalid format.")]
        [System.ComponentModel.DataAnnotations.StringLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
        public required string Email { get; set; }

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
        /// Gets the persistent authorization role for the user.
        /// This value is serialized in server responses but cannot be set by a client request.
        /// </summary>
        public UserRole Role { get; }

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
