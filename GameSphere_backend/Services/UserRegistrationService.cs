using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using GameSphere_backend.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        AppDbContext context,
        ILogger<UserRegistrationService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<UserRegistrationService>.Instance;
    }

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

        if (!await ValidateRegistrationAsync(user, response))
        {
            return response;
        }

        try
        {
            var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(user.Password, 13);
            var createdUser = UserMapper.UserToModel(user);

            if (createdUser == null)
            {
                response.Success = false;
                response.Message = "Error converting to Model";
                response.Type = "BadRequest";
                return response;
            }

            createdUser.HashedPassword = passwordHash;
            createdUser.HasLocalPassword = true;
            ConversionValidate.ValidateModel(createdUser);

            await _context.Users.AddAsync(createdUser);
            await _context.SaveChangesAsync();

            var createdUserDto = UserMapper.UserToDto(createdUser);
            if (createdUserDto == null)
            {
                response.Success = false;
                response.Message = "Error converting to DTO";
                response.Type = "BadRequest";
                return response;
            }

            response.Data = createdUserDto;
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

    private async Task<bool> ValidateRegistrationAsync(RegisterUserRequest user, ServiceResponse<UserDto> response)
    {
        if (user == null)
        {
            response.Success = false;
            response.Message = string.Empty;
            response.Type = "BadRequest";
            return false;
        }

        if (!await IsEmailAvailable(user.Email))
        {
            response.Success = false;
            response.Message = "Email is already in use";
            response.Type = "BadRequest";
            return false;
        }

        if (!Enum.IsDefined(typeof(Gender), user.Gender))
        {
            response.Success = false;
            response.Message = "Gender has an invalid value";
            response.Type = "BadRequest";
            return false;
        }

        return ValidatePassword(user.Password, response);
    }

    private static bool ValidatePassword(string password, ServiceResponse<UserDto> response)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            response.Success = false;
            response.Message = "Password is required";
            response.Type = "BadRequest";
            return false;
        }

        if (password.Trim().Length is < 8 or > 128)
        {
            response.Success = false;
            response.Message = "Password must contain between 8 and 128 characters";
            response.Type = "BadRequest";
            return false;
        }

        return true;
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
}
