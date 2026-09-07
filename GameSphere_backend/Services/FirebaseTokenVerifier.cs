using FirebaseAdmin;
using FirebaseAdmin.Auth;
using GameSphere_backend.Interfaces;
using GameSphere_backend.ServicesResponses;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class FirebaseTokenVerifier : IFirebaseTokenVerifier
{
    private readonly string _projectId;
    private readonly Lazy<FirebaseAuth> _firebaseAuth;
    private readonly ILogger<FirebaseTokenVerifier> _logger;

    public FirebaseTokenVerifier(IConfiguration configuration, ILogger<FirebaseTokenVerifier>? logger = null)
    {
        _projectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Configuration key 'Firebase:ProjectId' is required.");
        _logger = logger ?? NullLogger<FirebaseTokenVerifier>.Instance;

        _firebaseAuth = new Lazy<FirebaseAuth>(() =>
        {
            // VerifyIdTokenAsync only checks the token's signature against Google's public
            // certificates and never authenticates outbound calls with this credential, so a
            // real service account is not required here — only that Credential is non-null.
            var app = FirebaseApp.GetInstance("[DEFAULT]")
                ?? FirebaseApp.Create(new AppOptions
                {
                    ProjectId = _projectId,
                    Credential = GoogleCredential.FromAccessToken("unused-id-token-verification-only")
                });
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
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase ID token rejected: {ErrorCode}", ex.AuthErrorCode);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification misconfigured.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error verifying Firebase ID token.");
            return null;
        }
    }
}
