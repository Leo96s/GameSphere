using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using GameSphere_backend.Security;
using GameSphere_backend.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services
{
    /// <summary>
    /// Service class for handling user-related operations including creation, retrieval, 
    /// updating, deletion, authentication, and password management
    /// </summary>
    public class UserServices : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserServices> _logger;

        /// <summary>
        /// Initializes a new instance of the UserServices class.
        /// </summary>
        /// <param name="context">The database context for user data access</param>
        /// <param name="authService">The authentication service for token generation</param>
        /// <param name="emailService">The email service for sending notifications</param>
        /// <param name="logger">The logger for internal failures.</param>
        /// <exception cref="ArgumentNullException">Thrown when authService or emailService is null</exception>
        public UserServices(
            AppDbContext context,
            IAuthService authService,
            IEmailService emailService,
            ILogger<UserServices>? logger = null)
        {
            this._context = context;
            this._authService = authService ?? throw new ArgumentNullException(nameof(_authService));
            this._emailService = emailService ?? throw new ArgumentNullException(nameof(_emailService));
            _logger = logger ?? NullLogger<UserServices>.Instance;
        }

        /// <summary>
        /// Retrives a user by their unique identifier
        /// </summary>
        /// <param name="id">The ID of the user to retrieve</param>
        /// <returns>A ServiceResponse containing the UserDto if found, or an error message if not.</returns>
        public async Task<ServiceResponse<UserDto>> GetUserByIdAsync(int id)
        {
            if (_context == null)
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "DB context is Missing",
                    Type = "NotFound"
                };

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "User was not found!",
                    Type = "NotFound"
                };

            var u = UserMapper.UserToDto(user);

            if (u == null)
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "Error mapping user.",
                    Type = "BadRequest"
                };

            return new ServiceResponse<UserDto>
            {
                Success = true,
                Data = u,
                Type = "Ok"
            };
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="user">The UserDto containing user information to create.</param>
        /// <returns>A ServiceResponse indicating success or failure of the operation, including the created user if successful.</returns>
        public async Task<ServiceResponse<UserDto>> CreateNewUserAsync(RegisterUserRequest user)
        {
            var response = new ServiceResponse<UserDto>();

            if (_context == null)
            {
                response.Success = false;
                response.Message = string.Empty;
                response.Type = "NotFound";
                return response;
            }

            if (user == null)
            {
                response.Success = false;
                response.Message = string.Empty;
                response.Type = "BadRequest";
                return response;
            }

            if (!await IsEmailAvailable(user.Email))
            {
                response.Success = false;
                response.Message = "Email is already in use";
                response.Type = "BadRequest";
                return response;
            }
            if (!Enum.IsDefined(typeof(Gender), user.Gender))
            {
                response.Success = false;
                response.Message = "Gender has an invalid value";
                response.Type = "BadRequest";
                return response;
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                response.Success = false;
                response.Message = "Password is required";
                response.Type = "BadRequest";
                return response;
            }

            if (user.Password.Trim().Length is < 8 or > 128)
            {
                response.Success = false;
                response.Message = "Password must contain between 8 and 128 characters";
                response.Type = "BadRequest";
                return response;
            }

            try { 

                string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(user.Password, 13);
            

                var u = UserMapper.UserToModel(user);


                if (u == null)
                {
                    response.Success = false;
                    response.Message = "Error converting to Model";
                    response.Type = "BadRequest";
                    return response;
                }

                u.HashedPassword = passwordHash;
                ConversionValidate.ValidateModel(u);
                
                
                await _context.Users.AddAsync(u);
                await _context.SaveChangesAsync();

                var createdUserDTO = UserMapper.UserToDto(u);
                if (createdUserDTO == null)
                {
                    response.Success = false;
                    response.Message = "Error converting to DTO";
                    response.Type = "BadRequest";
                    return response;
                }

                response.Data = createdUserDTO;
                response.Success = true;
                response.Message = "User successfully created";
                response.Type = "Created";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create a user account.");
                response.Success = false;
                response.Message = "The user account could not be created.";
                response.Type = "BadRequest";
            }

            return response;
        }

        /// <summary>
        /// Checks if an email address is available for registration.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if the email is available, false if already in use or invalid.</returns>
        private async Task<Boolean> IsEmailAvailable(string email)
        {

            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalizedEmail = EmailNormalizer.Normalize(email);
            return !await _context.Users.AnyAsync(user => user.Email == normalizedEmail);
        }

        /// <summary>
        /// Deletes a user from the system by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>A ServiceResponse indicating success or failure of the deletion operation.</returns>
        public async Task<ServiceResponse<string>> DeleteUserAsync(int id)
        {
            var response = new ServiceResponse<string>();

            try
            {
                if (_context == null)
                {
                    response.Success = false;
                    response.Message = "DB context is missing.";
                    response.Type = "NotFound";
                    return response;
                }

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "The user was not found.";
                    response.Type = "NotFound";
                    return response;
                }

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "User and related data deleted successfully.";
                response.Data = $"User with ID {id} deleted.";
                response.Type = "NoContent";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user {UserId}.", id);
                response.Success = false;
                response.Message = "The user account could not be deleted.";
                response.Type = "BadRequest";
            }

            return response;
        }

        /// <summary>
        /// Updates an existing user's information.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="updatedUser">The UserDto containing updated user information.</param>
        /// <returns>A ServiceResponse containing the updated UserDto if successful, or error information if failed.</returns>
        public async Task<ServiceResponse<UserDto>> EditUserAsync(int id, UpdateUserRequest updatedUser)
        {
            var response = new ServiceResponse<UserDto>();

            try
            {
                if (updatedUser == null)
                {
                    response.Success = false;
                    response.Message = "User data is required.";
                    response.Type = "BadRequest";
                    return response;
                }

                if (_context == null)
                {
                    response.Success = false;
                    response.Message = "DB context is missing.";
                    response.Type = "NotFound";
                    return response;
                }

                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    response.Type = "NotFound";

                    return response;
                }

                if (string.IsNullOrWhiteSpace(updatedUser.FirstName)
                    || string.IsNullOrWhiteSpace(updatedUser.Email)
                    || !new EmailAddressAttribute().IsValid(updatedUser.Email)
                    || updatedUser.FirstName.Length > 100
                    || updatedUser.LastName?.Length > 100)
                {
                    response.Success = false;
                    response.Message = "Profile data is invalid.";
                    response.Type = "BadRequest";
                    return response;
                }

                if (updatedUser.Password is not null && updatedUser.Password.Length is < 8 or > 128)
                {
                    response.Success = false;
                    response.Message = "Password must contain between 8 and 128 characters.";
                    response.Type = "BadRequest";
                    return response;
                }

                if (!existingUser.Email.Equals(updatedUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (!await IsEmailAvailable(updatedUser.Email))
                    {
                        response.Success = false;
                        response.Message = "The email is already in use.";
                        response.Type = "BadRequest";
                        return response;
                    }
                }

                // Validar se o gender fornecido existe
                if (!Enum.IsDefined(typeof(Gender), updatedUser.Gender))
                {
                    response.Success = false;
                    response.Message = "Invalid gender.";
                    response.Type = "BadRequest";
                    return response;
                }


                // Atualizar os dados do usuário
                existingUser.FirstName = updatedUser.FirstName.Trim();
                existingUser.LastName = updatedUser.LastName?.Trim();
                existingUser.Email = EmailNormalizer.Normalize(updatedUser.Email);
                existingUser.Gender = updatedUser.Gender;
                existingUser.Image = updatedUser.Image;

                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(updatedUser.Password, 13);
                    existingUser.HashedPassword = passwordHash;
                    existingUser.AuthVersion++;
                }

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                // Converter para DTO
                var userDTO = UserMapper.UserToDto(existingUser);

                if (userDTO == null)
                {
                    response.Success = false;
                    response.Message = "Conversion to UserDTO failed.";
                    response.Type = "BadRequest";
                    return response;
                }

                response.Data = userDTO;
                response.Success = true;
                response.Message = "User updated successfully.";
                response.Type = "Ok";
            }
            catch (ValidationException ve)
            {
                response.Success = false;
                _logger.LogWarning(ve, "Invalid user profile update for user {UserId}.", id);
                response.Message = "Profile data is invalid.";
                response.Type = "BadRequest";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {UserId}.", id);
                response.Success = false;
                response.Message = "The user profile could not be updated.";
                response.Type = "BadRequest";
            }

            return response;
        }

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        /// <param name="request">The LoginRequest containing user credentials.</param>
        /// <returns>A ServiceResponse containing a LoginResponse with JWT token and user data if authentication succeeds.</returns>
        public async Task<ServiceResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var response = new ServiceResponse<LoginResponse>();

            try
            {
                if (_context == null)
                {
                    response.Success = false;
                    response.Message = "DB context is missing.";
                    response.Type = "NotFound";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    response.Success = false;
                    response.Message = "Email is required.";
                    response.Type = "BadRequest";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    response.Success = false;
                    response.Message = "Password is required.";
                    response.Type = "BadRequest";
                    return response;
                }

                var normalizedEmail = EmailNormalizer.Normalize(request.Email);
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail);

                if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.HashedPassword))
                {
                    response.Success = false;
                    response.Message = "The authentication failed! Please check your credentials.";
                    response.Type = "Unauthorized";
                    return response;
                }

                if (!user.isActive)
                {
                    response.Success = false;
                    response.Message = "Account is deactivated. Please check your email for the activation link.";
                    response.Type = "BadRequest";
                    return response;
                }

                var authenticatedUserDTO = UserMapper.UserToDto(user);

                if (authenticatedUserDTO == null)
                {
                    response.Success = false;
                    response.Message = "Error while mapping the UserDTO.";
                    response.Type = "BadRequest";
                    return response;
                }

                var token = _authService.GenerateToken(user.Id.ToString(), user.Email, user.Role, user.AuthVersion);

                response.Data = new LoginResponse
                {
                    token = token,
                    user = authenticatedUserDTO
                };
                response.Success = true;
                response.Message = "Login successful.";
                response.Type = "Ok";
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Invalid login request.");
                response.Success = false;
                response.Message = "Invalid login request.";
                response.Type = "BadRequest";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while logging in.");
                response.Success = false;
                response.Message = "Authentication is temporarily unavailable.";
                response.Type = "BadRequest";
            }

            return response;
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>A ServiceResponse containing the UserDto if found, or an error message if not.</returns>
        public async Task<ServiceResponse<UserDto>> GetUserByEmailAsync(string email)
        {
            if (_context == null)
            {
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "DB context is missing.",
                    Type = "NotFound"
                };
            }

            var normalizedEmail = EmailNormalizer.Normalize(email);
            var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Email == normalizedEmail);
            if (user == null)
            {
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "User was not found!",
                    Type = "NotFound"
                };
            }

            return new ServiceResponse<UserDto>
            {
                Success = true,
                Data = UserMapper.UserToDto(user),
                Type = "Ok"
            };
        }

        /// <summary>
        /// Sends a password reset code to the specified email address.
        /// </summary>
        /// <param name="email">The email address to send the reset code to.</param>
        /// <returns>A ServiceResponse indicating success or failure of the operation.</returns>
        public async Task<ServiceResponse<bool>> SendPasswordResetCode(string email)
        {
            var response = new ServiceResponse<bool>();

            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Email address is invalid.";
                response.Type = "BadRequest";
                return response;
            }

            email = EmailNormalizer.Normalize(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                response.Success = true;
                response.Data = true;
                response.Message = "If an account exists for this email, recovery instructions will be sent.";
                response.Type = "Ok";
                return response;
            }

            var resetCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            user.ResetCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(resetCode, 10);
            user.ResetCodeAttempts = 0;
            user.ResetCodeExpiration = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            string emailBody = $"<h3>Redefinição de Senha</h3>" +
                       $"<p>Seu código de recuperação é: <strong>{resetCode}</strong></p>" +
                       $"<p>Este código expira em 15 minutos.</p>";

            // Enviar o e-mail
            bool emailSent = await _emailService.SendEmailAsync(email, "Redefinição de Senha", emailBody);

            if (!emailSent)
            {
                _logger.LogError("Password reset email delivery failed for a registered account.");
            }

            response.Success = true;
            response.Data = true;
            response.Message = "If an account exists for this email, recovery instructions will be sent.";
            response.Type = "Ok";
            return response;
        }

        /// <summary>
        /// Validates a password reset code for a user.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="resetCode">The reset code to validate.</param>
        /// <returns>A ServiceResponse indicating whether the reset code is valid.</returns>
        public async Task<ServiceResponse<bool>> ValidateResetCode(string email, string resetCode)
        {
            var response = new ServiceResponse<bool>();

            if (string.IsNullOrWhiteSpace(email)
                || !new EmailAddressAttribute().IsValid(email)
                || string.IsNullOrWhiteSpace(resetCode)
                || !System.Text.RegularExpressions.Regex.IsMatch(resetCode, "^[0-9]{6}$"))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }

            email = EmailNormalizer.Normalize(email);
            var normalizedEmail = EmailNormalizer.Normalize(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }

            if (!await IsResetCodeValidAsync(user, resetCode))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }

            response.Success = true;
            response.Data = true;
            response.Message = "Valid reset code!";
            response.Type = "Ok";
            return response;
        }

        /// <summary>
        /// Resets a user's password using a valid reset code.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="resetCode">The valid reset code.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>A ServiceResponse indicating success or failure of the password reset.</returns>
        public async Task<ServiceResponse<bool>> ResetPassword(string email, string resetCode, string newPassword)
        {
            var response = new ServiceResponse<bool>();

            if (string.IsNullOrWhiteSpace(email)
                || !new EmailAddressAttribute().IsValid(email)
                || string.IsNullOrWhiteSpace(resetCode)
                || !System.Text.RegularExpressions.Regex.IsMatch(resetCode, "^[0-9]{6}$")
                || string.IsNullOrWhiteSpace(newPassword)
                || newPassword.Length is < 8 or > 128)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid password recovery data.";
                response.Type = "BadRequest";
                return response;
            }

            email = EmailNormalizer.Normalize(email);
            var normalizedEmail = EmailNormalizer.Normalize(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }

            if (!await IsResetCodeValidAsync(user, resetCode))
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }
            
            user.HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, 13);

            user.ResetCodeHash = null;
            user.ResetCodeAttempts = 0;
            user.ResetCodeExpiration = null;
            user.AuthVersion++;

            await _context.SaveChangesAsync();

            string emailBody = "<h3>Password changed</h3>" +
                       "<p>Your GameSphere password was changed. If this was not you, contact support.</p>";

            await _emailService.SendEmailAsync(email, "Password changed", emailBody);

            response.Success = true;
            response.Data = true;
            response.Message = "Password reset successfully!";
            response.Type = "Ok";
            return response;
        }

        public Task<ServiceResponse<UserDto>> EditUserAsync(int id, UserDto updatedUser) =>
            EditUserAsync(id, new UpdateUserRequest
            {
                FirstName = updatedUser.FirstName,
                LastName = updatedUser.LastName,
                Email = updatedUser.Email,
                Gender = updatedUser.Gender,
                Image = updatedUser.Image,
                Password = updatedUser.HashedPassword
            });

        private async Task<bool> IsResetCodeValidAsync(User user, string resetCode)
        {
            if (user.ResetCodeAttempts >= 5 ||
                string.IsNullOrWhiteSpace(user.ResetCodeHash) ||
                user.ResetCodeExpiration < DateTime.UtcNow)
            {
                return false;
            }

            bool valid;
            try
            {
                valid = BCrypt.Net.BCrypt.EnhancedVerify(resetCode, user.ResetCodeHash);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Invalid password reset hash encountered for user {UserId}.", user.Id);
                valid = false;
            }

            if (valid)
            {
                return true;
            }

            user.ResetCodeAttempts++;
            if (user.ResetCodeAttempts >= 5)
            {
                user.ResetCodeHash = null;
                user.ResetCodeExpiration = null;
            }

            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<ServiceResponse<LoginResponse>> SocialLoginAsync(FirebaseUserInfo firebaseUser)
        {
            var response = new ServiceResponse<LoginResponse>();

            try
            {
                if (string.IsNullOrWhiteSpace(firebaseUser.Uid) || string.IsNullOrWhiteSpace(firebaseUser.Email))
                {
                    response.Success = false;
                    response.Message = "The social login token is invalid.";
                    response.Type = "Unauthorized";
                    return response;
                }

                var email = EmailNormalizer.Normalize(firebaseUser.Email);
                var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.UID == firebaseUser.Uid);

                if (user is null)
                {
                    var emailUser = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Email == email);
                    if (emailUser is not null && emailUser.UID is not null)
                    {
                        response.Success = false;
                        response.Message = "The social login could not be completed.";
                        response.Type = "Conflict";
                        return response;
                    }

                    user = emailUser ?? CreateSocialUser(firebaseUser, email);
                    user.UID = firebaseUser.Uid;
                    user.Email = email;

                    if (emailUser is null)
                    {
                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    response.Success = false;
                    response.Message = "The social login could not be completed.";
                    response.Type = "Conflict";
                    return response;
                }

                if (!user.isActive)
                {
                    response.Success = false;
                    response.Message = "Account is deactivated. Please check your email for the activation link.";
                    response.Type = "BadRequest";
                    return response;
                }

                var authenticatedUserDTO = UserMapper.UserToDto(user);

                if (authenticatedUserDTO == null)
                {
                    response.Success = false;
                    response.Message = "Error while mapping the UserDTO.";
                    response.Type = "BadRequest";
                    return response;
                }

                var token = _authService.GenerateToken(user.Id.ToString(), user.Email, user.Role, user.AuthVersion);

                response.Data = new LoginResponse
                {
                    token = token,
                    user = authenticatedUserDTO
                };
                response.Success = true;
                response.Message = "Login successful.";
                response.Type = "Ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected social login error.");
                response.Success = false;
                response.Message = "The social login could not be completed.";
                response.Type = "BadRequest";
            }

            return response;
        }

        private static User CreateSocialUser(FirebaseUserInfo firebaseUser, string email)
        {
            var names = (firebaseUser.DisplayName ?? "GameSphere User")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new User
            {
                UID = firebaseUser.Uid,
                FirstName = names.FirstOrDefault() ?? "GameSphere",
                LastName = names.Length > 1 ? string.Join(' ', names.Skip(1)) : string.Empty,
                Email = email,
                HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(
                    Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                    13),
                RegistrationDate = DateTime.UtcNow,
                Gender = Gender.OUTRO,
                isActive = true,
                Role = UserRole.User,
                Level = 0,
                TotalPoints = 0,
            };
        }
    }
}
