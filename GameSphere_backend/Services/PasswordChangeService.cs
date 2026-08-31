using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class PasswordChangeService : IPasswordChangeService
{
    private const string ReauthenticationFailedMessage = "Current credentials could not be confirmed.";

    private readonly AppDbContext _context;
    private readonly AccountReauthenticator _reauthenticator;
    private readonly PasswordResetCodeService _resetCodeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordChangeService> _logger;

    public PasswordChangeService(
        AppDbContext context,
        AccountReauthenticator reauthenticator,
        PasswordResetCodeService resetCodeService,
        IEmailService emailService,
        ILogger<PasswordChangeService>? logger = null)
    {
        _context = context;
        _reauthenticator = reauthenticator;
        _resetCodeService = resetCodeService;
        _emailService = emailService;
        _logger = logger ?? NullLogger<PasswordChangeService>.Instance;
    }

    public async Task<ServiceResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (request.NewPassword.Trim().Length is < 8 or > 128)
        {
            return Failure("BadRequest", "New password must contain between 8 and 128 characters.");
        }

        var user = await _context.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (user is null)
        {
            return Failure("NotFound", "User not found.");
        }

        if (!await _reauthenticator.VerifyAsync(user, request.CurrentPassword, request.FirebaseIdToken))
        {
            return Failure("Unauthorized", ReauthenticationFailedMessage);
        }

        user.HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(request.NewPassword, 13);
        user.HasLocalPassword = true;
        user.AuthVersion++;
        _resetCodeService.ClearCode(user);

        await _context.SaveChangesAsync();

        var emailSent = await _emailService.SendEmailAsync(
            user.Email,
            "Password changed",
            "<h3>Password changed</h3><p>Your GameSphere password was changed. If this was not you, contact support.</p>");
        if (!emailSent)
        {
            _logger.LogError("Password change notification email delivery failed for user {UserId}.", user.Id);
        }

        return new ServiceResponse<bool> { Success = true, Data = true, Message = "Password changed successfully.", Type = "Ok" };
    }

    private static ServiceResponse<bool> Failure(string type, string message) => new()
    {
        Success = false,
        Data = false,
        Message = message,
        Type = type,
    };
}
