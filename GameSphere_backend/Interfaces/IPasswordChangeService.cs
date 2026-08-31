using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IPasswordChangeService
{
    Task<ServiceResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
