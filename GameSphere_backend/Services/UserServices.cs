using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        /// <summary>
        /// Initializes a new instance of the UserServices class.
        /// </summary>
        /// <param name="context">The database context for user data access</param>
        /// <param name="authService">The authentication service for token generation</param>
        /// <param name="emailService">The email service for sending notifications</param>
        /// <exception cref="ArgumentNullException">Thrown when authService or emailService is null</exception>
        public UserServices(AppDbContext context, IAuthService authService, IEmailService emailService)
        {
            this._context = context;
            this._authService = authService ?? throw new ArgumentNullException(nameof(_authService));
            this._emailService = emailService ?? throw new ArgumentNullException(nameof(_emailService));
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
        public async Task<ServiceResponse<UserDto>> CreateNewUserAsync(UserDto user)
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

            if (string.IsNullOrEmpty(user.HashedPassword))
            {
                response.Success = false;
                response.Message = "Password is required";
                response.Type = "BadRequest";
                return response;
            }

            try { 

                string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(user.HashedPassword, 13);
            

                var u = UserMapper.UserToModel(user);


                if (u == null)
                {
                    response.Success = false;
                    response.Message = "Error converting to Model";
                    response.Type = "BadRequest";
                    return response;
                }

                u.HashedPassword = passwordHash;
                
                
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
                response.Success = false;
                response.Message = ex.Message;
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

            return !await _context.Users.AnyAsync(user => user.Email == email);
        }

        /// <summary>
        /// Checks if a user exists with the specified UID and email.
        /// </summary>
        /// <param name="uid">The unique identifier from the external authentication provider.</param>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if a matching user exists, false otherwise.</returns>
        private async Task<Boolean> UserExist(string uid, string email)
        {
            if (string.IsNullOrWhiteSpace(uid)) return false;

            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _context.Users.AnyAsync(user => (user.Email == email && user.UID == uid));
        }

        /// <summary>
        /// Verifies the availability of an email address for registration.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>A ServiceResponse indicating whether the email is available, with appropriate messages.</returns>
        public async Task<ServiceResponse<bool>> CheckEmailAvailabilityAsync(string email)

        {
            var response = new ServiceResponse<bool>();

            try
            {
                if (_context == null)
                {
                    response.Success = false;
                    response.Message = "DB context is missing.";
                    response.Type = "NotFound";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Success = false;
                    response.Message = "Email cannot be null or empty.";
                    response.Type = "BadRequest";
                    return response;
                }

                // Verifica a disponibilidade do email
                var emailAvailable = await IsEmailAvailable(email);

                response.Data = emailAvailable;
                response.Success = true;
                response.Message = emailAvailable
                ? "Email is available."
                : "Email is already in use.";
                response.Type = "Ok";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while checking email availability.";
                response.Type = "BadRequest";
            }

            return response;
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
                response.Success = false;
                response.Message = "An error occurred while deleting the user.";
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
        public async Task<ServiceResponse<UserDto>> EditUserAsync(int id, UserDto updatedUser)
        {
            var response = new ServiceResponse<UserDto>();

            try
            {
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

                if (!existingUser.Email.Equals(updatedUser.Email))
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
                existingUser.FirstName = updatedUser.FirstName;
                existingUser.LastName = updatedUser.LastName;
                existingUser.Email = updatedUser.Email;
                existingUser.Gender = updatedUser.Gender;
                existingUser.Image = updatedUser.Image;

                if (!string.IsNullOrEmpty(updatedUser.HashedPassword))
                {
                    string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(updatedUser.HashedPassword, 13);
                    existingUser.HashedPassword = passwordHash;
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
                response.Message = ve.Message;
                response.Type = "BadRequest";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while updating the user.";
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

                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

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

                var token = _authService.GenerateToken(authenticatedUserDTO.Id.ToString(), authenticatedUserDTO.Email);

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
                response.Success = false;
                response.Message = ex.Message;
                response.Type = "BadRequest";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred during login.";
                response.Type = "BadRequest";
            }

            return response;
        }

        /// <summary>
        /// Checks if a user exists with the specified external provider UID and email.
        /// </summary>
        /// <param name="uid">The unique identifier from the external authentication provider.</param>
        /// <param name="email">The email address to check.</param>
        /// <returns>A ServiceResponse indicating whether the user exists.</returns>
        public async Task<ServiceResponse<bool>> CheckExistsUserExtern(string uid, string email)

        {
            var response = new ServiceResponse<bool>();

            try
            {
                if (_context == null)
                {
                    response.Success = false;
                    response.Message = "DB context is missing.";
                    response.Type = "NotFound";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Success = false;
                    response.Message = "Email cannot be null or empty.";
                    response.Type = "BadRequest";
                    return response;
                }

                // Verifica a disponibilidade do email
                var emailAvailable = await UserExist(uid, email);

                response.Data = emailAvailable;
                response.Success = true;
                response.Message = emailAvailable
                ? "User exist"
                : "User not exist";
                response.Type = "Ok";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while checking email availability.";
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
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "DB context is Missing",
                    Type = "NotFound"
                };

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "User was not found!",
                    Type = "NotFound"
                };

            var u = UserMapper.UserToDto(user);

            return new ServiceResponse<UserDto>
            {
                Success = true,
                Data = u,
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if(user == null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "User not found!";
                response.Type = "Not Found";
                return response;
            }

            var resetCode = new Random().Next(100000, 999999).ToString();

            user.ResetCode = resetCode;
            user.ResetCodeExpiration = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            string emailBody = $"<h3>Redefinição de Senha</h3>" +
                       $"<p>Seu código de recuperação é: <strong>{resetCode}</strong></p>" +
                       $"<p>Este código expira em 15 minutos.</p>";

            // Enviar o e-mail
            bool emailSent = await _emailService.SendEmailAsync(email, "Redefinição de Senha", emailBody);

            if (!emailSent)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Failed to send email.";
                response.Type = "BadRequest";
                return response;
            }

            response.Success = true;
            response.Data = true;
            response.Message = "Reset code sent successfully!";
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "User not found!";
                response.Type = "NotFound";
                return response;
            }

            if (user.ResetCode != resetCode || user.ResetCodeExpiration < DateTime.UtcNow)
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "User not found!";
                response.Type = "NotFound";
                return response;
            }

            if (user.ResetCode != resetCode || user.ResetCodeExpiration < DateTime.UtcNow)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Invalid or expired reset code!";
                response.Type = "BadRequest";
                return response;
            }
            
            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            user.ResetCode = null;
            user.ResetCodeExpiration = null;

            await _context.SaveChangesAsync();

            string emailBody = $"<h3>Senha alterada</h3>" +
                       $"<p>A sua senha foi altera se não foi você redefine a senha o mais depressa possível.</p>";

            // Enviar o e-mail
            bool emailSent = await _emailService.SendEmailAsync(email, "Aviso de Senha alterada", emailBody);

            if (!emailSent)
            {
                response.Success = false;
                response.Data = false;
                response.Message = "Failed to send email.";
                response.Type = "BadRequest";
                return response;
            }

            response.Success = true;
            response.Data = true;
            response.Message = "Password reset successfully!";
            response.Type = "Ok";
            return response;
        }

        /// <summary>
        /// Authenticates a user using external provider credentials (UID and email).
        /// </summary>
        /// <param name="uid">The unique identifier from the external authentication provider.</param>
        /// <param name="email">The email address associated with the external account.</param>
        /// <returns>A ServiceResponse containing a LoginResponse with JWT token and user data if authentication succeeds.</returns>
        public async Task<ServiceResponse<LoginResponse>> SocialLoginAsync(string uid, string email)
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

                if (string.IsNullOrWhiteSpace(email))
                {
                    response.Success = false;
                    response.Message = "Email is required.";
                    response.Type = "BadRequest";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(uid))
                {
                    response.Success = false;
                    response.Message = "Password is required.";
                    response.Type = "BadRequest";
                    return response;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => (u.Email == email && u.UID == uid));

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User dont exist!";
                    response.Type = "NotFound";
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

                var token = _authService.GenerateToken(authenticatedUserDTO.Id.ToString(), authenticatedUserDTO.Email);

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
                response.Success = false;
                response.Message = ex.Message;
                response.Type = "BadRequest";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Type = "BadRequest";
            }

            return response;
        }
    }
}
