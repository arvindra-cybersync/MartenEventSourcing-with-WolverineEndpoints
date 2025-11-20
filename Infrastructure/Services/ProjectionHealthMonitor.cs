using Marten;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.Projections;

namespace Infrastructure.Services;

public class ProjectionHealthMonitor : BackgroundService
{
    private readonly IDocumentStore _store;
    private readonly ILogger<ProjectionHealthMonitor> _logger;

    // How often to check projection lag
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    // Threshold — if lag exceeds this, trigger rebuild
    private const long RebuildLagThreshold = 1000;

    public ProjectionHealthMonitor(IDocumentStore store, ILogger<ProjectionHealthMonitor> logger)
    {
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🩺 Projection Health Monitor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRebuildProjectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking projection lag.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckAndRebuildProjectionsAsync(CancellationToken ct)
    {
        await using var session = _store.LightweightSession();

        var latestEventSeq = await session.QueryAsync<long?>("select max(seq_id) from mt_events;");
        var latestSeqId = latestEventSeq.FirstOrDefault() ?? 0;

        if (latestSeqId == 0)
        {
            _logger.LogInformation("No events yet, skipping health check.");
            return;
        }

        var progressList = await session.QueryAsync<(string ShardName, long LastSeqId)>(
            "select shard_name, last_seq_id from mt_event_progression;"
        );

        foreach (var (shard, lastSeq) in progressList)
        {
            var lag = latestSeqId - lastSeq;

            if (lag > RebuildLagThreshold)
            {
                _logger.LogWarning("⚠️ Projection '{Shard}' is lagging by {Lag} events. Triggering rebuild...", shard, lag);

                using var daemon = await _store.BuildProjectionDaemonAsync();

                // Identify which projection to rebuild based on shard name
                if (shard.Contains("order_summary", StringComparison.OrdinalIgnoreCase))
                {
                    await daemon.RebuildProjectionAsync(typeof(OrderSummaryProjection), ct);
                }
                else if (shard.Contains("product_sales", StringComparison.OrdinalIgnoreCase))
                {
                    await daemon.RebuildProjectionAsync(typeof(ProductSalesProjection), ct);
                }

                _logger.LogInformation("✅ Projection '{Shard}' rebuild triggered.", shard);
            }
            else
            {
                _logger.LogInformation("✅ Projection '{Shard}' healthy. Lag: {Lag} events.", shard, lag);
            }
        }
    }
}
