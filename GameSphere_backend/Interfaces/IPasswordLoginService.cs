using GameSphere_backend.ServicesResponses;
using Microsoft.AspNetCore.Identity.Data;

namespace GameSphere_backend.Interfaces;

public interface IPasswordLoginService
{
    Task<ServiceResponse<LoginResponse>> LoginAsync(LoginRequest request);
}
