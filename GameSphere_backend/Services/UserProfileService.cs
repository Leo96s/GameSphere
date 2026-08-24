using System.ComponentModel.DataAnnotations;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        AppDbContext context,
        ILogger<UserProfileService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<UserProfileService>.Instance;
    }

    public async Task<ServiceResponse<UserDto>> EditUserAsync(int id, UpdateUserRequest updatedUser)
    {
        var response = new ServiceResponse<UserDto>();

        try
        {
            if (!ValidateProfileRequest(updatedUser, response))
            {
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

            if (!await ValidateUniqueEmailAsync(existingUser.Email, updatedUser.Email, response))
            {
                return response;
            }

            ApplyProfileUpdate(existingUser, updatedUser);

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            return BuildSuccessResponse(existingUser, response);
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

    private static bool ValidateProfileRequest(UpdateUserRequest updatedUser, ServiceResponse<UserDto> response)
    {
        if (updatedUser == null)
        {
            response.Success = false;
            response.Message = "User data is required.";
            response.Type = "BadRequest";
            return false;
        }

        if (!HasValidProfileData(updatedUser))
        {
            response.Success = false;
            response.Message = "Profile data is invalid.";
            response.Type = "BadRequest";
            return false;
        }

        if (updatedUser.Password is not null && updatedUser.Password.Length is < 8 or > 128)
        {
            response.Success = false;
            response.Message = "Password must contain between 8 and 128 characters.";
            response.Type = "BadRequest";
            return false;
        }

        if (!Enum.IsDefined(typeof(Gender), updatedUser.Gender))
        {
            response.Success = false;
            response.Message = "Invalid gender.";
            response.Type = "BadRequest";
            return false;
        }

        return true;
    }

    private static bool HasValidProfileData(UpdateUserRequest updatedUser) =>
        !string.IsNullOrWhiteSpace(updatedUser.FirstName)
        && !string.IsNullOrWhiteSpace(updatedUser.Email)
        && new EmailAddressAttribute().IsValid(updatedUser.Email)
        && updatedUser.FirstName.Length <= 100
        && (updatedUser.LastName is null || updatedUser.LastName.Length <= 100);

    private async Task<bool> ValidateUniqueEmailAsync(
        string existingEmail,
        string updatedEmail,
        ServiceResponse<UserDto> response)
    {
        if (existingEmail.Equals(updatedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (await IsEmailAvailable(updatedEmail))
        {
            return true;
        }

        response.Success = false;
        response.Message = "The email is already in use.";
        response.Type = "BadRequest";
        return false;
    }

    private async Task<bool> IsEmailAvailable(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = EmailNormalizer.Normalize(email);
        return !await _context.Users.AnyAsync(user => user.Email == normalizedEmail);
    }

    private static void ApplyProfileUpdate(Models.BackendModels.User existingUser, UpdateUserRequest updatedUser)
    {
        existingUser.FirstName = updatedUser.FirstName.Trim();
        if (updatedUser.LastName is not null)
        {
            existingUser.LastName = updatedUser.LastName.Trim();
        }

        existingUser.Email = EmailNormalizer.Normalize(updatedUser.Email);
        existingUser.Gender = updatedUser.Gender;
        existingUser.Image = updatedUser.Image;

        if (!string.IsNullOrEmpty(updatedUser.Password))
        {
            existingUser.HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(updatedUser.Password, 13);
            existingUser.AuthVersion++;
        }
    }

    private static ServiceResponse<UserDto> BuildSuccessResponse(
        Models.BackendModels.User existingUser,
        ServiceResponse<UserDto> response)
    {
        var userDto = UserMapper.UserToDto(existingUser);
        if (userDto == null)
        {
            response.Success = false;
            response.Message = "Conversion to UserDTO failed.";
            response.Type = "BadRequest";
            return response;
        }

        response.Data = userDto;
        response.Success = true;
        response.Message = "User updated successfully.";
        response.Type = "Ok";
        return response;
    }
}
