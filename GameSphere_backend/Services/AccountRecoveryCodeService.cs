using System.Security.Cryptography;
using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class AccountRecoveryCodeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccountRecoveryCodeService> _logger;

    public AccountRecoveryCodeService(
        AppDbContext context,
        ILogger<AccountRecoveryCodeService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<AccountRecoveryCodeService>.Instance;
    }

    public string CreateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public void AssignCode(User user, string recoveryCode, DateTime utcNow)
    {
        user.RecoveryCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(recoveryCode, 10);
        user.RecoveryCodeAttempts = 0;
        user.RecoveryCodeExpiration = utcNow.AddMinutes(15);
    }

    public async Task<bool> ValidateAsync(User user, string recoveryCode)
    {
        if (!HasUsableRecoveryCode(user))
        {
            return false;
        }

        var valid = VerifyRecoveryCode(user, recoveryCode);
        if (valid)
        {
            return true;
        }

        user.RecoveryCodeAttempts++;
        if (user.RecoveryCodeAttempts >= 5)
        {
            ClearCodeForFailedAttempts(user);
        }

        await _context.SaveChangesAsync();
        return false;
    }

    public void ClearCode(User user)
    {
        user.RecoveryCodeHash = null;
        user.RecoveryCodeAttempts = 0;
        user.RecoveryCodeExpiration = null;
    }

    private static bool HasUsableRecoveryCode(User user) =>
        user.RecoveryCodeAttempts < 5 &&
        !string.IsNullOrWhiteSpace(user.RecoveryCodeHash) &&
        user.RecoveryCodeExpiration >= DateTime.UtcNow;

    private static void ClearCodeForFailedAttempts(User user)
    {
        user.RecoveryCodeHash = null;
        user.RecoveryCodeExpiration = null;
    }

    private bool VerifyRecoveryCode(User user, string recoveryCode)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(recoveryCode, user.RecoveryCodeHash);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Invalid account recovery hash encountered for user {UserId}.", user.Id);
            return false;
        }
    }
}
