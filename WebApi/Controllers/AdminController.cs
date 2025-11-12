using Microsoft.AspNetCore.Mvc;
using Marten;
using Marten.Events.Daemon;
using Infrastructure.Projections;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IDocumentStore _store;

    public AdminController(IDocumentStore store)
    {
        _store = store;
    }

    [HttpPost("projections/rebuild")]
    public async Task<IActionResult> RebuildProjections(CancellationToken ct)
    {
        using var daemon = await _store.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync(typeof(OrderSummaryProjection), ct);

        return Ok(new
        {
            message = "Projection rebuild triggered successfully",
            projection = nameof(OrderSummaryProjection),
            time = DateTime.UtcNow
        });
    }

    [HttpPost("projections/multistream/rebuild")]
    public async Task<IActionResult> RebuildMultiStreamProjections(CancellationToken ct)
    {
        using var daemon = await _store.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync(typeof(ProductSalesProjection), ct);

        return Ok(new
        {
            message = $"Projection '{nameof(ProductSalesProjection)}' rebuild started successfully.",
            time = DateTime.UtcNow
        });
    }

    [HttpPost("projections/rebuild/all")]
    public async Task<IActionResult> RebuildAll(CancellationToken ct)
    {
        using var daemon = await _store.BuildProjectionDaemonAsync();

        var projections = new[]
        {
            typeof(OrderSummaryProjection),
            typeof(ProductSalesProjection)
        };

        foreach (var projectionType in projections)
            await daemon.RebuildProjectionAsync(projectionType, ct);

        return Ok(new { message = "All projections rebuild started", time = DateTime.UtcNow });
    }

    [HttpGet("projections/status")]
    public IActionResult Status()
    {
        return Ok(new { daemon = "ready", time = DateTime.UtcNow });
    }

    [HttpGet("projections/progress")]
    public async Task<IActionResult> GetProjectionProgress(CancellationToken ct)
    {
        await using var session = _store.LightweightSession();

        var progress = await session.QueryAsync<ProjectionProgress>(
            "select shard_name, last_seq_id, timestamp from mt_event_progression order by shard_name;"
        );

        return Ok(progress);
    }

    private record ProjectionProgress(string Shard_Name, long Last_Seq_Id, DateTime? Timestamp);

}
