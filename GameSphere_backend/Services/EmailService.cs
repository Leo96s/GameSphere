using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using Microsoft.Extensions.Options;

namespace GameSphere_backend.Services
{
    /// <summary>
    /// Service responsible for sending emails using SMTP protocol.
    /// </summary>
    /// <remarks>
    /// This service implements the IEmailService interface and provides functionality
    /// for sending HTML emails to users. It uses configuration from EmailSettings
    /// to connect to the SMTP server.
    /// </remarks>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        /// <summary>
        /// Initializes a new instance of the EmailService class.
        /// </summary>
        /// <param name="emailSettings">The email configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when emailSettings is null.</exception>
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Sends an email to the specified recipient.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The HTML content of the email.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains:
        /// <c>true</c> if the email was sent successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method configures and sends an email using the following settings from EmailSettings:
        /// - SMTP server address
        /// - SMTP port number
        /// - Sender credentials (username and password)
        /// - SSL/TLS configuration
        /// - Sender email address
        /// 
        /// All emails are sent as HTML format.
        /// </remarks>
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
                {
                    Port = _emailSettings.SmtpPort,
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSSL
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
