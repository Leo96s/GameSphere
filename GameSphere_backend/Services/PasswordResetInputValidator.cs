using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GameSphere_backend.Services;

public interface IPasswordResetInputValidator
{
    string? ValidateEmail(string email);

    string? ValidateRequest(string email, string resetCode, string? newPassword = null);
}

public sealed class PasswordResetInputValidator : IPasswordResetInputValidator
{
    private const string InvalidEmailMessage = "Email address is invalid.";
    private const string InvalidResetCodeMessage = "Invalid or expired reset code!";
    private const string InvalidPasswordRecoveryDataMessage = "Invalid password recovery data.";

    public string? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            return InvalidEmailMessage;
        }

        return null;
    }

    public string? ValidateRequest(string email, string resetCode, string? newPassword = null)
    {
        if (ValidateEmail(email) is not null ||
            string.IsNullOrWhiteSpace(resetCode) ||
            !Regex.IsMatch(resetCode, "^[0-9]{6}$"))
        {
            return newPassword is null ? InvalidResetCodeMessage : InvalidPasswordRecoveryDataMessage;
        }

        if (newPassword is not null && (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length is < 8 or > 128))
        {
            return InvalidPasswordRecoveryDataMessage;
        }

        return null;
    }
}
