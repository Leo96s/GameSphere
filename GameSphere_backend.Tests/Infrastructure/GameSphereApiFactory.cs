using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GameSphere_backend.Tests.Infrastructure;

public sealed class GameSphereApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlFixture _database;

    public GameSphereApiFactory(PostgreSqlFixture database)
    {
        _database = database;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:GameSphereDB", _database.ConnectionString);
        builder.UseSetting("JwtSettings:SecretKey", "test-secret-with-at-least-thirty-two-bytes");
        builder.UseSetting("JwtSettings:Issuer", "gamesphere-tests");
        builder.UseSetting("JwtSettings:Audience", "gamesphere-tests");
        builder.UseSetting("JwtSettings:ExpirationMinutes", "60");
        builder.UseSetting("Firebase:ProjectId", "gamesphere-9f7dc");
        builder.UseSetting("EmailSettings:SmtpServer", "smtp.example.test");
        builder.UseSetting("EmailSettings:SmtpPort", "2525");
        builder.UseSetting("EmailSettings:SenderEmail", "no-reply@example.test");
        builder.UseSetting("EmailSettings:SenderName", "GameSphere Tests");
        builder.UseSetting("EmailSettings:Username", "test-user");
        builder.UseSetting("EmailSettings:Password", "test-email-password");
        builder.UseSetting("EmailSettings:EnableSSL", "false");
        builder.UseSetting("InitialAdmin:Email", "admin@example.test");
        builder.UseSetting("InitialAdmin:Password", "Test-only-admin-password-123");
    }
}
