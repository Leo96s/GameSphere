using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IUserQueryService
{
    Task<ServiceResponse<UserDto>> GetUserByIdAsync(int id);

    Task<ServiceResponse<UserDto>> GetUserByEmailAsync(string email);
}
