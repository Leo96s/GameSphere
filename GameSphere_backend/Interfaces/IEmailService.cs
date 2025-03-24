namespace GameSphere_backend.Interfaces
{
    /// <summary>
    /// Defines the contract for email sending services within the application.
    /// </summary>
    /// <remarks>
    /// This interface provides a standard way to implement email functionality
    /// across different parts of the system, allowing for easy substitution
    /// of email service implementations.
    /// </remarks>
    public interface IEmailService
    {
        /// <summary>
        /// Asynchronously sends an email to the specified recipient.
        /// </summary>
        /// <param name="to">The email address of the recipient. Must be a valid email format.</param>
        /// <param name="subject">The subject line of the email. Cannot be null or empty.</param>
        /// <param name="body">The content of the email. Supports HTML content.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. The task result contains:
        /// <c>true</c> if the email was sent successfully; <c>false</c> if sending failed.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="to"/>, <paramref name="subject"/>, 
        /// or <paramref name="body"/> is null or empty.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when <paramref name="to"/> is not a valid email address format.
        /// </exception>
        /// <remarks>
        /// Implementations should:
        /// 1. Validate all input parameters
        /// 2. Handle SMTP or other email protocol exceptions internally
        /// 3. Return false rather than throwing exceptions for expected failure cases
        /// 4. Support HTML content in the email body
        /// </remarks>
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
