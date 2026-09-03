using System.Security.Cryptography;
using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameSphere_backend.Services;

public sealed class AccountAnonymizer : IAccountAnonymizer
{
    private const int RecoveryWindowDays = 30;

    private readonly AppDbContext _context;

    public AccountAnonymizer(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> AnonymizeExpiredAccountsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var cutoff = utcNow.AddDays(-RecoveryWindowDays);

        var expiredAccounts = await _context.Users
            .Where(user => !user.isActive
                && !user.IsAnonymized
                && user.DeactivatedAt != null
                && user.DeactivatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var user in expiredAccounts)
        {
            user.Email = $"deleted-user-{user.Id}@anonymized.gamesphere.invalid";
            user.FirstName = "Deleted";
            user.LastName = string.Empty;
            user.Image = null;
            user.UID = null;
            user.HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                13);
            user.HasLocalPassword = false;
            user.ResetCodeHash = null;
            user.ResetCodeAttempts = 0;
            user.ResetCodeExpiration = null;
            user.PendingEmail = null;
            user.PendingEmailCodeHash = null;
            user.PendingEmailCodeAttempts = 0;
            user.PendingEmailCodeExpiration = null;
            user.RecoveryCodeHash = null;
            user.RecoveryCodeAttempts = 0;
            user.RecoveryCodeExpiration = null;
            user.IsAnonymized = true;
        }

        if (expiredAccounts.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return expiredAccounts.Count;
    }
}
