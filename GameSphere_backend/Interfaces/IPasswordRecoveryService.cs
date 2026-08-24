using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IPasswordRecoveryService
{
    Task<ServiceResponse<bool>> SendPasswordResetCode(string email);

    Task<ServiceResponse<bool>> ValidateResetCode(string email, string resetCode);

    Task<ServiceResponse<bool>> ResetPassword(string email, string resetCode, string newPassword);
}
