using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Interfaces;

public interface IFirebaseTokenVerifier
{
    Task<FirebaseUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
