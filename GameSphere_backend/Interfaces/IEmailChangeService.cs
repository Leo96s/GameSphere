using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IEmailChangeService
{
    Task<ServiceResponse<bool>> RequestEmailChangeAsync(int userId, RequestEmailChangeRequest request);

    Task<ServiceResponse<bool>> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeRequest request);
}
