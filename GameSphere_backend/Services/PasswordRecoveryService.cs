using GameSphere_backend.Data;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.BackendModels;
using GameSphere_backend.Security;
using GameSphere_backend.ServicesResponses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    private const string GenericRecoveryMessage = "If an account exists for this email, recovery instructions will be sent.";
    private const string InvalidResetCodeMessage = "Invalid or expired reset code!";
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetInputValidator _inputValidator;
    private readonly PasswordResetCodeService _resetCodeService;
    private readonly ILogger<PasswordRecoveryService> _logger;

    public PasswordRecoveryService(
        AppDbContext context,
        IEmailService emailService,
        IPasswordResetInputValidator inputValidator,
        PasswordResetCodeService resetCodeService,
        ILogger<PasswordRecoveryService>? logger = null)
    {
        _context = context;
        _emailService = emailService;
        _inputValidator = inputValidator;
        _resetCodeService = resetCodeService;
        _logger = logger ?? NullLogger<PasswordRecoveryService>.Instance;
    }

    public async Task<ServiceResponse<bool>> SendPasswordResetCode(string email)
    {
        var validationMessage = _inputValidator.ValidateEmail(email);
        if (validationMessage is not null)
        {
            return CreateResponse(false, false, validationMessage, "BadRequest");
        }

        var normalizedEmail = EmailNormalizer.Normalize(email);
        var user = await FindUserByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return CreateResponse(true, true, GenericRecoveryMessage, "Ok");
        }

        var resetCode = _resetCodeService.CreateCode();
        _resetCodeService.AssignCode(user, resetCode, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var emailSent = await _emailService.SendEmailAsync(
            normalizedEmail,
            "Redefinição de Senha",
            CreateResetCodeEmail(resetCode));
        if (!emailSent)
        {
            _logger.LogError("Password reset email delivery failed for a registered account.");
        }

        return CreateResponse(true, true, GenericRecoveryMessage, "Ok");
    }

    public async Task<ServiceResponse<bool>> ValidateResetCode(string email, string resetCode)
    {
        var validationMessage = _inputValidator.ValidateRequest(email, resetCode);
        if (validationMessage is not null)
        {
            return CreateResponse(false, false, validationMessage, "BadRequest");
        }

        var user = await FindUserByEmailAsync(EmailNormalizer.Normalize(email));
        if (user is null || !await _resetCodeService.ValidateAsync(user, resetCode))
        {
            return CreateResponse(false, false, InvalidResetCodeMessage, "BadRequest");
        }

        return CreateResponse(true, true, "Valid reset code!", "Ok");
    }

    public async Task<ServiceResponse<bool>> ResetPassword(string email, string resetCode, string newPassword)
    {
        var validationMessage = _inputValidator.ValidateRequest(email, resetCode, newPassword);
        if (validationMessage is not null)
        {
            return CreateResponse(false, false, validationMessage, "BadRequest");
        }

        var normalizedEmail = EmailNormalizer.Normalize(email);
        var user = await FindUserByEmailAsync(normalizedEmail);
        if (user is null || !await _resetCodeService.ValidateAsync(user, resetCode))
        {
            return CreateResponse(false, false, InvalidResetCodeMessage, "BadRequest");
        }

        await ApplyPasswordResetAsync(user, normalizedEmail, newPassword);
        return CreateResponse(true, true, "Password reset successfully!", "Ok");
    }

    private Task<User?> FindUserByEmailAsync(string normalizedEmail) =>
        _context.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail);

    private async Task ApplyPasswordResetAsync(User user, string email, string newPassword)
    {
        user.HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, 13);
        user.HasLocalPassword = true;
        _resetCodeService.ClearCode(user);
        user.AuthVersion++;

        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(email, "Password changed", "<h3>Password changed</h3>" +
            "<p>Your GameSphere password was changed. If this was not you, contact support.</p>");
    }

    private static string CreateResetCodeEmail(string resetCode) =>
        $"<h3>Redefinição de Senha</h3>" +
        $"<p>Seu código de recuperação é: <strong>{resetCode}</strong></p>" +
        $"<p>Este código expira em 15 minutos.</p>";

    private static ServiceResponse<bool> CreateResponse(bool success, bool data, string message, string type) => new()
    {
        Success = success,
        Data = data,
        Message = message,
        Type = type,
    };
}
