using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Mappers;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services;

public sealed class UserQueryService : IUserQueryService
{
    private readonly AppDbContext _context;

    public UserQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<UserDto>> GetUserByIdAsync(int id)
    {
        if (_context == null)
        {
            return new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "DB context is Missing",
                Type = "NotFound"
            };
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "User was not found!",
                Type = "NotFound"
            };
        }

        var userDto = UserMapper.UserToDto(user);
        if (userDto == null)
        {
            return new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "Error mapping user.",
                Type = "BadRequest"
            };
        }

        return new ServiceResponse<UserDto>
        {
            Success = true,
            Data = userDto,
            Type = "Ok"
        };
    }

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
}
