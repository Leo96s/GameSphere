namespace GameSphere_backend.Interfaces;

public interface IAccountAnonymizer
{
    Task<int> AnonymizeExpiredAccountsAsync(DateTime utcNow, CancellationToken cancellationToken);
}
