using System.Security.Cryptography;
using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class PasswordResetCodeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PasswordResetCodeService> _logger;

    public PasswordResetCodeService(
        AppDbContext context,
        ILogger<PasswordResetCodeService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<PasswordResetCodeService>.Instance;
    }

    public string CreateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public void AssignCode(User user, string resetCode, DateTime utcNow)
    {
        user.ResetCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(resetCode, 10);
        user.ResetCodeAttempts = 0;
        user.ResetCodeExpiration = utcNow.AddMinutes(15);
    }

    public async Task<bool> ValidateAsync(User user, string resetCode)
    {
        if (!HasUsableResetCode(user))
        {
            return false;
        }

        var valid = VerifyResetCode(user, resetCode);
        if (valid)
        {
            return true;
        }

        user.ResetCodeAttempts++;
        if (user.ResetCodeAttempts >= 5)
        {
            ClearCodeForFailedAttempts(user);
        }

        await _context.SaveChangesAsync();
        return false;
    }

    public void ClearCode(User user)
    {
        user.ResetCodeHash = null;
        user.ResetCodeAttempts = 0;
        user.ResetCodeExpiration = null;
    }

    private static bool HasUsableResetCode(User user) =>
        user.ResetCodeAttempts < 5 &&
        !string.IsNullOrWhiteSpace(user.ResetCodeHash) &&
        user.ResetCodeExpiration >= DateTime.UtcNow;

    private static void ClearCodeForFailedAttempts(User user)
    {
        user.ResetCodeHash = null;
        user.ResetCodeExpiration = null;
    }

    private bool VerifyResetCode(User user, string resetCode)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(resetCode, user.ResetCodeHash);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Invalid password reset hash encountered for user {UserId}.", user.Id);
            return false;
        }
    }
}
