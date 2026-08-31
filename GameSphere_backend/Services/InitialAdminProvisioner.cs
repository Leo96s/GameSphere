using GameSphere_backend.Data;
using GameSphere_backend.Enums;
using GameSphere_backend.Models.BackendModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameSphere_backend.Services;

public sealed class InitialAdminProvisioner
{
    private readonly AppDbContext _context;

    public InitialAdminProvisioner(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureAdminAsync(
        InitialAdminConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await FindUserByEmailAsync(configuration.Email, cancellationToken);

        if (existingUser is not null)
        {
            await PromoteToAdminAsync(existingUser, cancellationToken);
            return;
        }

        await CreateAdminAsync(configuration, cancellationToken);
    }

    private async Task CreateAdminAsync(
        InitialAdminConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(CreateAdmin(configuration), cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmailUniqueConstraintViolation(exception))
        {
            await PromoteConcurrentlyCreatedUserAsync(configuration.Email, cancellationToken);
        }
    }

    private async Task PromoteConcurrentlyCreatedUserAsync(
        string email,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        var concurrentlyCreatedUser = await FindUserByEmailAsync(email, cancellationToken);

        if (concurrentlyCreatedUser is null)
        {
            throw new InvalidOperationException("The configured administrator could not be recovered after a unique email conflict.");
        }

        await PromoteToAdminAsync(concurrentlyCreatedUser, cancellationToken);
    }

    private async Task PromoteToAdminAsync(User user, CancellationToken cancellationToken)
    {
        user.Role = UserRole.Admin;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
        => await _context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    private static User CreateAdmin(InitialAdminConfiguration configuration) => new()
    {
        Email = configuration.Email,
        FirstName = "Administrator",
        LastName = "Account",
        HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(configuration.Password, 13),
        HasLocalPassword = true,
        RegistrationDate = DateTime.UtcNow,
        Gender = Gender.OUTRO,
        isActive = true,
        Role = UserRole.Admin,
    };

    private static bool IsEmailUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Users_Email",
        };
}
