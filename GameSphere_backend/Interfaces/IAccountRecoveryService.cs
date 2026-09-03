using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IAccountRecoveryService
{
    Task<ServiceResponse<bool>> RequestRecoveryCode(string email);

    Task<ServiceResponse<bool>> RecoverAccount(string email, string code);
}
