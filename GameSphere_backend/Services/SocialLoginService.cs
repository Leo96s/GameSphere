using System.Security.Cryptography;
using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class SocialLoginService : ISocialLoginService
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<SocialLoginService> _logger;

    public SocialLoginService(
        AppDbContext context,
        IAuthService authService,
        ILogger<SocialLoginService>? logger = null)
    {
        _context = context;
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? NullLogger<SocialLoginService>.Instance;
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
            var user = await ResolveSocialUserAsync(firebaseUser, email, response);
            if (user is null)
            {
                return response;
            }

            if (!user.isActive)
            {
                response.Success = false;
                response.Message = "Account is deactivated. Please check your email for the activation link.";
                response.Type = "BadRequest";
                return response;
            }

            return BuildLoginResponse(user, response);
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

    private async Task<User?> ResolveSocialUserAsync(
        FirebaseUserInfo firebaseUser,
        string email,
        ServiceResponse<LoginResponse> response)
    {
        var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.UID == firebaseUser.Uid);
        if (user is null)
        {
            return await ResolveUserWithoutMatchingUidAsync(firebaseUser, email, response);
        }

        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return user;
        }

        response.Success = false;
        response.Message = "The social login could not be completed.";
        response.Type = "Conflict";
        return null;
    }

    private async Task<User?> ResolveUserWithoutMatchingUidAsync(
        FirebaseUserInfo firebaseUser,
        string email,
        ServiceResponse<LoginResponse> response)
    {
        var emailUser = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Email == email);
        if (emailUser is not null && emailUser.UID is not null)
        {
            response.Success = false;
            response.Message = "The social login could not be completed.";
            response.Type = "Conflict";
            return null;
        }

        var user = emailUser ?? CreateSocialUser(firebaseUser, email);
        user.UID = firebaseUser.Uid;
        user.Email = email;

        if (emailUser is null)
        {
            await _context.Users.AddAsync(user);
        }

        await _context.SaveChangesAsync();
        return user;
    }

    private ServiceResponse<LoginResponse> BuildLoginResponse(
        User user,
        ServiceResponse<LoginResponse> response)
    {
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
