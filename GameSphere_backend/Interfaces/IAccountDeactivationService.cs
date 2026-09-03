using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IAccountDeactivationService
{
    Task<ServiceResponse<bool>> DeactivateAsync(int userId, DeactivateAccountRequest request);
}
