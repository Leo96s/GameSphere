using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;

namespace GameSphere_backend.Security;

/// <summary>
/// Confirms recent proof of identity for sensitive credential operations (GAM-16).
/// </summary>
/// <remarks>
/// A local account (no linked Firebase UID) proves identity with its current password.
/// A social-only account (linked UID, no local password ever set) proves identity with
/// a freshly verified Firebase ID token. A dual account (linked UID and a local password
/// already set) may use either.
/// </remarks>
public sealed class AccountReauthenticator
{
    private readonly IFirebaseTokenVerifier _firebaseTokenVerifier;

    public AccountReauthenticator(IFirebaseTokenVerifier firebaseTokenVerifier)
    {
        _firebaseTokenVerifier = firebaseTokenVerifier;
    }

    public async Task<bool> VerifyAsync(User user, string? currentPassword, string? firebaseIdToken)
    {
        var canUsePassword = user.HasLocalPassword && !string.IsNullOrWhiteSpace(currentPassword);
        if (canUsePassword && VerifyPassword(user, currentPassword!))
        {
            return true;
        }

        var canUseFirebase = user.UID is not null && !string.IsNullOrWhiteSpace(firebaseIdToken);
        if (canUseFirebase && await VerifyFirebaseAsync(user, firebaseIdToken!))
        {
            return true;
        }

        return false;
    }

    private static bool VerifyPassword(User user, string currentPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(currentPassword, user.HashedPassword);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> VerifyFirebaseAsync(User user, string firebaseIdToken)
    {
        var firebaseUser = await _firebaseTokenVerifier.VerifyAsync(firebaseIdToken);
        return firebaseUser is not null && firebaseUser.Uid == user.UID;
    }
}
