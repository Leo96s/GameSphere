using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class EmailChangeService : IEmailChangeService
{
    private const string ReauthenticationFailedMessage = "Current credentials could not be confirmed.";
    private const string InvalidOrExpiredCodeMessage = "Invalid or expired confirmation code!";

    private readonly AppDbContext _context;
    private readonly AccountReauthenticator _reauthenticator;
    private readonly EmailChangeCodeService _codeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailChangeService> _logger;

    public EmailChangeService(
        AppDbContext context,
        AccountReauthenticator reauthenticator,
        EmailChangeCodeService codeService,
        IEmailService emailService,
        ILogger<EmailChangeService>? logger = null)
    {
        _context = context;
        _reauthenticator = reauthenticator;
        _codeService = codeService;
        _emailService = emailService;
        _logger = logger ?? NullLogger<EmailChangeService>.Instance;
    }

    public async Task<ServiceResponse<bool>> RequestEmailChangeAsync(int userId, RequestEmailChangeRequest request)
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

        if (user.UID is not null && !user.HasLocalPassword)
        {
            return Failure(
                "BadRequest",
                "Set a local password for this account before changing its email address.");
        }

        var normalizedEmail = EmailNormalizer.Normalize(request.NewEmail);
        if (string.Equals(normalizedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("BadRequest", "The new email must be different from the current one.");
        }

        if (await _context.Users.AnyAsync(candidate => candidate.Email == normalizedEmail))
        {
            return Failure("BadRequest", "The email is already in use.");
        }

        var code = _codeService.CreateCode();
        _codeService.AssignPendingEmail(user, normalizedEmail, code, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var emailSent = await _emailService.SendEmailAsync(
            normalizedEmail,
            "Confirm your new email",
            $"<h3>Confirm your new email</h3><p>Your confirmation code is: <strong>{code}</strong></p><p>This code expires in 15 minutes.</p>");
        if (!emailSent)
        {
            _logger.LogError("Email change confirmation delivery failed for user {UserId}.", user.Id);
        }

        return new ServiceResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "A confirmation code was sent to the new email address.",
            Type = "Ok",
        };
    }

    public async Task<ServiceResponse<bool>> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null)
        {
            return Failure("NotFound", "User not found.");
        }

        if (!await _codeService.ValidateAsync(user, request.Code))
        {
            return Failure("BadRequest", InvalidOrExpiredCodeMessage);
        }

        var previousEmail = user.Email;
        var newEmail = user.PendingEmail!;

        user.Email = newEmail;
        if (user.UID is not null)
        {
            // The Firebase-linked identity no longer matches the GameSphere email;
            // unlink it so future social logins cannot silently diverge (see GAM-16).
            user.UID = null;
        }

        _codeService.ClearPendingEmail(user);
        user.AuthVersion++;
        await _context.SaveChangesAsync();

        await NotifyEmailChangedAsync(previousEmail, newEmail);

        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Email changed successfully.", Type = "Ok" };
    }

    private async Task NotifyEmailChangedAsync(string previousEmail, string newEmail)
    {
        const string subject = "Email changed";
        const string body = "<h3>Email changed</h3><p>Your GameSphere account email was changed. If this was not you, contact support.</p>";

        var previousNotified = await _emailService.SendEmailAsync(previousEmail, subject, body);
        var newNotified = await _emailService.SendEmailAsync(newEmail, subject, body);
        if (!previousNotified || !newNotified)
        {
            _logger.LogError("Email change notification delivery failed for {PreviousEmail} -> {NewEmail}.", previousEmail, newEmail);
        }
    }

    private static ServiceResponse<bool> Failure(string type, string message) => new()
    {
        Success = false,
        Data = false,
        Message = message,
        Type = type,
    };
}
