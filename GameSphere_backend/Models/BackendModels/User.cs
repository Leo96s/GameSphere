using GameSphere_backend.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Models.BackendModels
{
    /// <summary>
    /// Represents a user entity in the GameSphere system.
    /// </summary>
    /// <remarks>
    /// This class serves as the main user model containing all user-related data,
    /// including personal information, authentication details, and game-related statistics.
    /// It also includes navigation properties for related entities.
    /// </remarks>
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        /// <value>The primary key of the user in the database.</value>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier from external authentication providers.
        /// </summary>
        /// <value>The UID provided by third-party authentication services.</value>
        public string? UID { get; set; }

        /// <summary>
        /// Gets or sets the hashed password for the user.
        /// </summary>
        /// <value>BCrypt hashed password string.</value>
        [MinLength(8, ErrorMessage = "The field 'HashedPassword' must be at least 8 characters long.")]
        public required string HashedPassword { get; set; }

        /// <summary>
        /// Gets or sets the first name of the user.
        /// </summary>
        /// <value>The user's first name (100 character limit).</value>
        [Required(ErrorMessage = "The field 'FirstName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'FirstName' has to be less than 100 characters.")]
        public required string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the user.
        /// </summary>
        /// <value>The user's last name (100 character limit).</value>
        [Required(ErrorMessage = "The field 'LastName' is required.")]
        [MaxLength(100, ErrorMessage = "The field 'LastName' has to be less than 100 characters.")]
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        /// <value>The user's unique email address.</value>
        [Required(ErrorMessage = "The campo 'Email' is required.")]
        [EmailAddress(ErrorMessage = "The email has an invalid format.")]
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the date when the user registered.
        /// </summary>
        /// <value>The UTC datetime when the user account was created.</value>
        [Required(ErrorMessage = "The field 'RegistrationDate' is required.")]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Gets or sets the gender of the user.
        /// </summary>
        /// <value>The user's gender from the Gender enum.</value>
        [Required(ErrorMessage = "The field 'Gender' is required.")]
        public Gender Gender { get; set; }

        /// <summary>
        /// Gets or sets the URL or path to the user's profile image.
        /// </summary>
        /// <value>Nullable string representing the image location.</value>
        public string? Image { get; set; }

        /// <summary>
        /// Gets or sets the user's current level in the system.
        /// </summary>
        /// <value>The numerical representation of the user's level.</value>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the total points accumulated by the user.
        /// </summary>
        /// <value>The sum of all points earned by the user.</value>
        public long TotalPoints { get; set; }

        /// <summary>
        /// Gets or sets whether the user account is active.
        /// </summary>
        /// <value>True if the account is active, false if deactivated.</value>
        public bool isActive { get; set; }

        /// <summary>
        /// Gets or sets the persistent authorization role for the user.
        /// </summary>
        /// <value>The user role, which defaults to <see cref="UserRole.User"/>.</value>
        public UserRole Role { get; set; } = UserRole.User;

        public int AuthVersion { get; set; }

        /// <summary>
        /// Gets or sets the authentication token for the user.
        /// </summary>
        /// <value>Nullable string containing the JWT token.</value>
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the authentication token.
        /// </summary>
        /// <value>Nullable DateTime representing when the token expires.</value>
        public DateTime? TokenExpDate { get; set; }

        /// <summary>
        /// Gets or sets the hashed password reset code for the user.
        /// </summary>
        /// <value>Nullable string containing the temporary reset code.</value>
        public string? ResetCodeHash { get; set; }

        public int ResetCodeAttempts { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the password reset code.
        /// </summary>
        /// <value>Nullable DateTime representing when the reset code expires.</value>
        public DateTime? ResetCodeExpiration { get; set; }

        /// <summary>
        /// Gets or sets whether the user knows their own local password.
        /// </summary>
        /// <value>
        /// False for a social-only account, whose <see cref="HashedPassword"/> is an
        /// unknown, randomly generated placeholder. Becomes true once the user sets a
        /// local password explicitly.
        /// </value>
        public bool HasLocalPassword { get; set; }

        /// <summary>
        /// Gets or sets the new email address awaiting confirmation.
        /// </summary>
        public string? PendingEmail { get; set; }

        /// <summary>
        /// Gets or sets the hashed confirmation code for the pending email change.
        /// </summary>
        public string? PendingEmailCodeHash { get; set; }

        public int PendingEmailCodeAttempts { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the pending email change code.
        /// </summary>
        public DateTime? PendingEmailCodeExpiration { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the account was deactivated.
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the account's personal data has been anonymized.
        /// </summary>
        public bool IsAnonymized { get; set; }

        /// <summary>
        /// Gets or sets the hashed account recovery code.
        /// </summary>
        public string? RecoveryCodeHash { get; set; }

        public int RecoveryCodeAttempts { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the account recovery code.
        /// </summary>
        public DateTime? RecoveryCodeExpiration { get; set; }

        /// <summary>
        /// Gets or sets the collection of quizzes created by the user.
        /// </summary>
        /// <value>Collection of Quizz entities.</value>
        public ICollection<Quizz>? Quizzs { get; set; }

        /// <summary>
        /// Gets or sets the collection of games associated with the user.
        /// </summary>
        /// <value>Collection of Game entities.</value>
        public ICollection<Game>? Games { get; set; }

        /// <summary>
        /// Gets or sets the collection of achievements earned by the user.
        /// </summary>
        /// <value>Collection of Achievements entities.</value>
        public ICollection<Achievements>? Achievements { get; set; }

        /// <summary>
        /// Gets or sets the collection of scores achieved by the user.
        /// </summary>
        /// <value>Collection of Score entities.</value>
        public ICollection<Score>? Scores { get; set; }

    }
}
