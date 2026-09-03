using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class AccountRecoveryService : IAccountRecoveryService
{
    private const string GenericRecoveryMessage = "If a deactivated account exists for this email within the recovery window, recovery instructions will be sent.";
    private const string InvalidRecoveryCodeMessage = "Invalid or expired recovery code!";
    private const int RecoveryWindowDays = 30;

    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly AccountRecoveryCodeService _recoveryCodeService;
    private readonly ILogger<AccountRecoveryService> _logger;

    public AccountRecoveryService(
        AppDbContext context,
        IEmailService emailService,
        AccountRecoveryCodeService recoveryCodeService,
        ILogger<AccountRecoveryService>? logger = null)
    {
        _context = context;
        _emailService = emailService;
        _recoveryCodeService = recoveryCodeService;
        _logger = logger ?? NullLogger<AccountRecoveryService>.Instance;
    }

    public async Task<ServiceResponse<bool>> RequestRecoveryCode(string email)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var user = await FindUserByEmailAsync(normalizedEmail);
        var utcNow = DateTime.UtcNow;

        if (user is null || !IsEligibleForRecovery(user, utcNow))
        {
            return CreateResponse(true, true, GenericRecoveryMessage, "Ok");
        }

        var recoveryCode = _recoveryCodeService.CreateCode();
        _recoveryCodeService.AssignCode(user, recoveryCode, utcNow);
        await _context.SaveChangesAsync();

        var emailSent = await _emailService.SendEmailAsync(
            normalizedEmail,
            "Account recovery",
            CreateRecoveryCodeEmail(recoveryCode));
        if (!emailSent)
        {
            _logger.LogError("Account recovery email delivery failed for a deactivated account.");
        }

        return CreateResponse(true, true, GenericRecoveryMessage, "Ok");
    }

    public async Task<ServiceResponse<bool>> RecoverAccount(string email, string code)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var user = await FindUserByEmailAsync(normalizedEmail);
        var utcNow = DateTime.UtcNow;

        if (user is null || !IsEligibleForRecovery(user, utcNow) || !await _recoveryCodeService.ValidateAsync(user, code))
        {
            return CreateResponse(false, false, InvalidRecoveryCodeMessage, "BadRequest");
        }

        user.isActive = true;
        user.DeactivatedAt = null;
        user.AuthVersion++;
        _recoveryCodeService.ClearCode(user);

        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(
            normalizedEmail,
            "Account recovered",
            "<h3>Account recovered</h3><p>Your GameSphere account was recovered. If this was not you, contact support.</p>");

        return CreateResponse(true, true, "Account recovered successfully!", "Ok");
    }

    private Task<User?> FindUserByEmailAsync(string normalizedEmail) =>
        _context.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail);

    private static bool IsEligibleForRecovery(User user, DateTime utcNow) =>
        !user.isActive &&
        !user.IsAnonymized &&
        user.DeactivatedAt is not null &&
        user.DeactivatedAt.Value.AddDays(RecoveryWindowDays) >= utcNow;

    private static string CreateRecoveryCodeEmail(string recoveryCode) =>
        $"<h3>Account recovery</h3>" +
        $"<p>Your recovery code is: <strong>{recoveryCode}</strong></p>" +
        $"<p>This code expires in 15 minutes.</p>";

    private static ServiceResponse<bool> CreateResponse(bool success, bool data, string message, string type) => new()
    {
        Success = success,
        Data = data,
        Message = message,
        Type = type,
    };
}
