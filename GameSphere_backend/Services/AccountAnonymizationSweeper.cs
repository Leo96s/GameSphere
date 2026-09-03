using GameSphere_backend.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameSphere_backend.Services;

public sealed class AccountAnonymizationSweeper : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountAnonymizationSweeper> _logger;

    public AccountAnonymizationSweeper(
        IServiceScopeFactory scopeFactory,
        ILogger<AccountAnonymizationSweeper>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger ?? NullLogger<AccountAnonymizationSweeper>.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            using var scope = _scopeFactory.CreateScope();
            var anonymizer = scope.ServiceProvider.GetRequiredService<IAccountAnonymizer>();
            var count = await anonymizer.AnonymizeExpiredAccountsAsync(DateTime.UtcNow, stoppingToken);
            if (count > 0)
            {
                _logger.LogInformation("Anonymized {Count} expired accounts.", count);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
