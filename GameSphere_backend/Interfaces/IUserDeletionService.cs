using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IUserDeletionService
{
    Task<ServiceResponse<string>> DeleteUserAsync(int id);
}
