using System.ComponentModel.DataAnnotations;
using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class PasswordLoginService : IPasswordLoginService
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<PasswordLoginService> _logger;

    public PasswordLoginService(
        AppDbContext context,
        IAuthService authService,
        ILogger<PasswordLoginService>? logger = null)
    {
        _context = context;
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? NullLogger<PasswordLoginService>.Instance;
    }

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

            if (!ValidateLoginRequest(request, response))
            {
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

            var authenticatedUserDto = UserMapper.UserToDto(user);
            if (authenticatedUserDto == null)
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
                user = authenticatedUserDto
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

    private static bool ValidateLoginRequest(LoginRequest request, ServiceResponse<LoginResponse> response)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            response.Success = false;
            response.Message = "Email is required.";
            response.Type = "BadRequest";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            response.Success = false;
            response.Message = "Password is required.";
            response.Type = "BadRequest";
            return false;
        }

        return true;
    }
}
