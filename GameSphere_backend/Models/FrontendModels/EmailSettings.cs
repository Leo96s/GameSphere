using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Models.FrontendModels
{
    /// <summary>
    /// Represents the configuration settings for email service operations.
    /// </summary>
    /// <remarks>
    /// This class is typically populated from application configuration (appsettings.json)
    /// and used to configure the SMTP client for sending emails.
    /// </remarks>
    public class EmailSettings
    {
        /// <summary>
        /// Gets or sets the SMTP server hostname or IP address.
        /// </summary>
        /// <example>smtp.gmail.com</example>
        [Required(ErrorMessage = "SMTP server address is required")]
        public string SmtpServer { get; set; }

        /// <summary>
        /// Gets or sets the SMTP server port number.
        /// </summary>
        /// <example>587</example>
        [Required(ErrorMessage = "SMTP port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int SmtpPort { get; set; }

        /// <summary>
        /// Gets or sets the email address that will appear as the sender.
        /// </summary>
        /// <example>noreply@gamesphere.com</example>
        [Required(ErrorMessage = "Sender email is required")]
        [EmailAddress(ErrorMessage = "Invalid sender email format")]
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets the display name for the sender.
        /// </summary>
        /// <example>GameSphere Support</example>
        [Required(ErrorMessage = "Sender name is required")]
        [StringLength(100, ErrorMessage = "Sender name cannot exceed 100 characters")]
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets the username for SMTP authentication.
        /// </summary>
        /// <example>your-email@domain.com</example>
        [Required(ErrorMessage = "SMTP username is required")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for SMTP authentication.
        /// </summary>
        /// <remarks>
        /// This value should be stored securely and never logged.
        /// </remarks>
        [Required(ErrorMessage = "SMTP password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL/TLS encryption should be used.
        /// </summary>
        /// <remarks>
        /// Recommended to be true for production environments.
        /// </remarks>
        [Required(ErrorMessage = "SSL setting is required")]
        public bool EnableSSL { get; set; }
    }
}
