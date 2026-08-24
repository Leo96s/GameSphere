using System.ComponentModel.DataAnnotations;
using GameSphere_backend.Security;

namespace GameSphere_backend.Services;

public sealed record InitialAdminConfiguration(string Email, string Password)
{
    private const int MinimumPasswordLength = 8;

    public static InitialAdminConfiguration Load(IConfiguration configuration)
    {
        var email = GetConfiguredEmail(configuration);
        var password = GetConfiguredPassword(configuration);

        return new InitialAdminConfiguration(EmailNormalizer.Normalize(email), password);
    }

    private static string GetConfiguredEmail(IConfiguration configuration)
    {
        var email = configuration["InitialAdmin:Email"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            throw new InvalidOperationException("Configuration key 'InitialAdmin:Email' must contain a valid email address.");
        }

        return email;
    }

    private static string GetConfiguredPassword(IConfiguration configuration)
    {
        var password = configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            throw new InvalidOperationException($"Configuration key 'InitialAdmin:Password' must contain at least {MinimumPasswordLength} characters.");
        }

        return password;
    }
}
