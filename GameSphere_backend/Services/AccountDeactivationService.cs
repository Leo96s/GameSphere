using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class AccountDeactivationService : IAccountDeactivationService
{
    private const string ReauthenticationFailedMessage = "Current credentials could not be confirmed.";
    private const int RecoveryWindowDays = 30;

    private readonly AppDbContext _context;
    private readonly AccountReauthenticator _reauthenticator;
    private readonly AccountRecoveryCodeService _recoveryCodeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountDeactivationService> _logger;

    public AccountDeactivationService(
        AppDbContext context,
        AccountReauthenticator reauthenticator,
        AccountRecoveryCodeService recoveryCodeService,
        IEmailService emailService,
        ILogger<AccountDeactivationService>? logger = null)
    {
        _context = context;
        _reauthenticator = reauthenticator;
        _recoveryCodeService = recoveryCodeService;
        _emailService = emailService;
        _logger = logger ?? NullLogger<AccountDeactivationService>.Instance;
    }

    public async Task<ServiceResponse<bool>> DeactivateAsync(int userId, DeactivateAccountRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null)
        {
            return Failure("NotFound", "User not found.");
        }

        if (!await _reauthenticator.VerifyAsync(user, request.CurrentPassword, request.FirebaseIdToken))
        {
            return Failure("Unauthorized", ReauthenticationFailedMessage);
        }

        var deactivatedAt = DateTime.UtcNow;
        user.isActive = false;
        user.DeactivatedAt = deactivatedAt;
        user.AuthVersion++;

        var recoveryCode = _recoveryCodeService.CreateCode();
        _recoveryCodeService.AssignCode(user, recoveryCode, deactivatedAt);

        await _context.SaveChangesAsync();

        var recoverBy = deactivatedAt.AddDays(RecoveryWindowDays);
        var emailSent = await _emailService.SendEmailAsync(
            user.Email,
            "Account deactivated",
            CreateDeactivationEmail(recoveryCode, recoverBy));
        if (!emailSent)
        {
            _logger.LogError("Account deactivation notification email delivery failed for user {UserId}.", user.Id);
        }

        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Account deactivated successfully.", Type = "Ok" };
    }

    private static string CreateDeactivationEmail(string recoveryCode, DateTime recoverBy) =>
        $"<h3>Account deactivated</h3>" +
        $"<p>Your GameSphere account was deactivated. You can recover it until {recoverBy:yyyy-MM-dd} using the code: <strong>{recoveryCode}</strong></p>" +
        $"<p>After that date, your personal data will be permanently anonymized.</p>";

    private static ServiceResponse<bool> Failure(string type, string message) => new()
    {
        Success = false,
        Data = false,
        Message = message,
        Type = type,
    };
}
