using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface ISocialLoginService
{
    Task<ServiceResponse<LoginResponse>> SocialLoginAsync(FirebaseUserInfo firebaseUser);
}
