﻿using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Services
{
    public class UserServices : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService authService;
        public UserServices(AppDbContext context, IAuthService authService)
        {
            this._context = context;
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

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

            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(user.HashedPassword, 13);

            var u = UserMapper.UserToModel(user);

            try
            {

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
        /// Function that checks if an email is available to use, that is, is not present in the DB
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Returns false if already exists a similar email on the DB or the email parameter
        /// is null or empty or returns true if the email doesn't exist on the DB</returns>
        private async Task<Boolean> IsEmailAvailable(string email)
        {

            if (string.IsNullOrWhiteSpace(email))
                return false;

            return !await _context.Users.AnyAsync(user => user.Email == email);
        }

        /// <summary>
        /// Function that exists with the purpose of informing the user if his email is already registered
        /// on the plataform
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Returns a ServiceResponse with a response.Sucess=false and a message 
        /// if something is wrong or a response.Sucess=true and a mensage informing the user
        /// about his choice of email availability accordingly </returns>
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
        /// Function that deletes the user from the DB by his id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns a ServiceResponse with a response.Sucess=false and a message 
        /// if something is wrong or a response.Sucess=true and a sucess mensage</returns>
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
        /// Function that updates the user based on the updatedUser dto that it receives
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updatedUser"></param>
        /// <returns>Returns a ServiceResponse with a response.Sucess=false and a message 
        /// if something is wrong or a response.Sucess=true and a sucess mensage</returns>
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
        /// Function that makes the User login
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns a ServiceResponse with a response.Sucess=false and a message 
        /// if something is wrong or a response.Sucess=true with a sucess mensage and a
        /// LoginResponse with a jwt token and the user dto it belongs to</returns>
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


                response.Data = new LoginResponse
                {
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
    }
}
