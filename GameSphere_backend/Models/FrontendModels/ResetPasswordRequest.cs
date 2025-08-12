using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels
{
    /// <summary>
    /// Represents the request payload for resetting a user's password.
    /// </summary>
    /// <remarks>
    /// This class is used when a user submits their new password along with the
    /// verification code received via email to complete the password reset process.
    /// </remarks>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Gets or sets the email address of the user requesting password reset.
        /// </summary>
        /// <value>
        /// The email address associated with the user account.
        /// </value>
        /// <example>user@example.com</example>
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the verification code received by the user.
        /// </summary>
        /// <value>
        /// The 6-digit numeric code sent to the user's email for verification.
        /// </value>
        /// <example>123456</example>
        [Required(ErrorMessage = "Reset code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Reset code must be 6 digits")]
        public string ResetCode { get; set; }

        /// <summary>
        /// Gets or sets the new password for the user account.
        /// </summary>
        /// <value>
        /// The new password that will replace the current one.
        /// Must meet the system's password complexity requirements.
        /// </value>
        /// <example>NewSecurePassword123!</example>
        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
