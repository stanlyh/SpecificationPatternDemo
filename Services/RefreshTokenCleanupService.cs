using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SpecificationPatternDemo.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly RefreshTokenCleanupOptions _options;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger, IOptions<RefreshTokenCleanupOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        var retention = Math.Max(1, _options.RetentionDays);

        _logger.LogInformation("RefreshTokenCleanupService started. IntervalMinutes: {IntervalMinutes}, RetentionDays: {RetentionDays}", _options.IntervalMinutes, _options.RetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanExpiredTokens(retention, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning refresh tokens");
            }

            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("RefreshTokenCleanupService stopping");
    }

    private async Task CleanExpiredTokens(int retentionDays, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var threshold = DateTime.UtcNow.AddDays(-retentionDays);

        // Remove tokens that expired more than retentionDays ago or were revoked more than retentionDays ago
        var toRemove = await db.RefreshTokens
            .Where(r => (r.IsExpired && r.Expires <= threshold) || (r.IsRevoked && r.RevokedAt <= threshold))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (toRemove.Count == 0)
        {
            _logger.LogDebug("No expired refresh tokens to remove");
            return;
        }

        db.RefreshTokens.RemoveRange(toRemove);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed {Count} expired/revoked refresh tokens", toRemove.Count);
    }

    // Expose manual cleaning method for controller use
    public async Task<int> RunCleanupOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var retentionDays = Math.Max(1, _options.RetentionDays);
        var threshold = DateTime.UtcNow.AddDays(-retentionDays);

        var toRemove = await db.RefreshTokens
            .Where(r => (r.IsExpired && r.Expires <= threshold) || (r.IsRevoked && r.RevokedAt <= threshold))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (toRemove.Count == 0)
        {
            _logger.LogInformation("Manual cleanup found no tokens to remove");
            return 0;
        }

        db.RefreshTokens.RemoveRange(toRemove);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Manual cleanup removed {Count} tokens", toRemove.Count);
        return toRemove.Count;
    }
}
