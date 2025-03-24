namespace GameSphere_backend.Models.FrontendModels
{
    /// <summary>
    /// Represents a request to validate a password reset code for a user.
    /// </summary>
    /// <remarks>
    /// This model is used when a user submits the reset code they received via email
    /// to verify its validity before allowing a password reset.
    /// </remarks>
    public class ValidateResetCodeRequest
    {
        /// <summary>
        /// Gets or sets the email address of the user requesting password reset.
        /// </summary>
        /// <value>
        /// The email address associated with the user account.
        /// </value>
        /// <example>user@example.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the reset code that was sent to the user's email.
        /// </summary>
        /// <value>
        /// The 6-digit numeric code generated for password reset.
        /// </value>
        /// <example>123456</example>
        public string ResetCode { get; set; }
    }
}
