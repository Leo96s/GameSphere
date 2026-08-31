using System.Security.Cryptography;
using GameSphere_backend.Data;
using GameSphere_backend.Models.BackendModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class EmailChangeCodeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailChangeCodeService> _logger;

    public EmailChangeCodeService(
        AppDbContext context,
        ILogger<EmailChangeCodeService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<EmailChangeCodeService>.Instance;
    }

    public string CreateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public void AssignPendingEmail(User user, string pendingEmail, string code, DateTime utcNow)
    {
        user.PendingEmail = pendingEmail;
        user.PendingEmailCodeHash = BCrypt.Net.BCrypt.EnhancedHashPassword(code, 10);
        user.PendingEmailCodeAttempts = 0;
        user.PendingEmailCodeExpiration = utcNow.AddMinutes(15);
    }

    public async Task<bool> ValidateAsync(User user, string code)
    {
        if (!HasUsablePendingEmail(user))
        {
            return false;
        }

        if (VerifyCode(user, code))
        {
            return true;
        }

        user.PendingEmailCodeAttempts++;
        if (user.PendingEmailCodeAttempts >= 5)
        {
            ClearPendingEmailForFailedAttempts(user);
        }

        await _context.SaveChangesAsync();
        return false;
    }

    public void ClearPendingEmail(User user)
    {
        user.PendingEmail = null;
        user.PendingEmailCodeHash = null;
        user.PendingEmailCodeAttempts = 0;
        user.PendingEmailCodeExpiration = null;
    }

    private static bool HasUsablePendingEmail(User user) =>
        user.PendingEmailCodeAttempts < 5 &&
        !string.IsNullOrWhiteSpace(user.PendingEmail) &&
        !string.IsNullOrWhiteSpace(user.PendingEmailCodeHash) &&
        user.PendingEmailCodeExpiration >= DateTime.UtcNow;

    private static void ClearPendingEmailForFailedAttempts(User user)
    {
        user.PendingEmail = null;
        user.PendingEmailCodeHash = null;
        user.PendingEmailCodeExpiration = null;
    }

    private bool VerifyCode(User user, string code)
    {
        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(code, user.PendingEmailCodeHash);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Invalid pending email code hash encountered for user {UserId}.", user.Id);
            return false;
        }
    }
}
