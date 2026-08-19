using System.ComponentModel.DataAnnotations;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameSphere_backend.Services
{
    /// <summary>
    /// Creates or promotes the administrator configured for the application.
    /// </summary>
    public sealed class InitialAdminBootstrapper
    {
        private const int MinimumPasswordLength = 8;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialAdminBootstrapper"/> class.
        /// </summary>
        public InitialAdminBootstrapper(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Ensures that the configured user exists and has the administrator role.
        /// </summary>
        public async Task EnsureAdminAsync(CancellationToken cancellationToken = default)
        {
            var email = GetConfiguredEmail();
            var password = GetConfiguredPassword();
            var existingUser = await _context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (existingUser is not null)
            {
                await PromoteToAdminAsync(existingUser, cancellationToken);
                return;
            }

            var admin = new User
            {
                Email = email,
                FirstName = "Administrator",
                LastName = "Account",
                HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 13),
                RegistrationDate = DateTime.UtcNow,
                Gender = Gender.OUTRO,
                isActive = true,
                Role = UserRole.Admin,
            };

            await _context.Users.AddAsync(admin, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsEmailUniqueConstraintViolation(exception))
            {
                _context.ChangeTracker.Clear();
                var concurrentlyCreatedUser = await _context.Users
                    .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

                if (concurrentlyCreatedUser is null)
                {
                    throw;
                }

                await PromoteToAdminAsync(concurrentlyCreatedUser, cancellationToken);
            }
        }

        private async Task PromoteToAdminAsync(User user, CancellationToken cancellationToken)
        {
            if (user.Role == UserRole.Admin)
            {
                return;
            }

            user.Role = UserRole.Admin;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static bool IsEmailUniqueConstraintViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Users_Email",
            };

        private string GetConfiguredEmail()
        {
            var email = _configuration["InitialAdmin:Email"]?.Trim();

            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                throw new InvalidOperationException("Configuration key 'InitialAdmin:Email' must contain a valid email address.");
            }

            return email;
        }

        private string GetConfiguredPassword()
        {
            var password = _configuration["InitialAdmin:Password"];

            if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
            {
                throw new InvalidOperationException($"Configuration key 'InitialAdmin:Password' must contain at least {MinimumPasswordLength} characters.");
            }

            return password;
        }
    }
}
