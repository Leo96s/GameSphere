using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.ServicesResponses;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class UserDeletionService : IUserDeletionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserDeletionService> _logger;

    public UserDeletionService(
        AppDbContext context,
        ILogger<UserDeletionService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<UserDeletionService>.Instance;
    }

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
}
