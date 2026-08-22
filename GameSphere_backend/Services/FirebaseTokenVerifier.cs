using FirebaseAdmin;
using FirebaseAdmin.Auth;
using GameSphere_backend.Interfaces;
using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services;

public sealed class FirebaseTokenVerifier : IFirebaseTokenVerifier
{
    private readonly string _projectId;
    private readonly Lazy<FirebaseAuth> _firebaseAuth;

    public FirebaseTokenVerifier(IConfiguration configuration)
    {
        _projectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Configuration key 'Firebase:ProjectId' is required.");

        _firebaseAuth = new Lazy<FirebaseAuth>(() =>
        {
            var app = FirebaseApp.GetInstance("[DEFAULT]")
                ?? FirebaseApp.Create(new AppOptions { ProjectId = _projectId });
            return FirebaseAuth.GetAuth(app);
        });
    }

    public async Task<FirebaseUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var decodedToken = await _firebaseAuth.Value.VerifyIdTokenAsync(idToken, cancellationToken);
            var email = decodedToken.Claims.TryGetValue("email", out var emailValue)
                ? emailValue?.ToString()
                : null;
            var displayName = decodedToken.Claims.TryGetValue("name", out var nameValue)
                ? nameValue?.ToString()
                : null;

            return string.IsNullOrWhiteSpace(decodedToken.Uid) || string.IsNullOrWhiteSpace(email)
                ? null
                : new FirebaseUserInfo(decodedToken.Uid, email, displayName);
        }
        catch (FirebaseAuthException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
