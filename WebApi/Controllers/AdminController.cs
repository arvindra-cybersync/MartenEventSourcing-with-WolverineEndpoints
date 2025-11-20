using Marten;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Projections;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IDocumentStore _store;

    public AdminController(IDocumentStore store) => _store = store;

    [HttpPost("projections/rebuild")]
    public async Task<IActionResult> RebuildProjections(CancellationToken ct)
    {
        using var daemon = await _store.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync(typeof(OrderSummaryProjection), ct);
        return Ok(new { message = "Projection rebuild triggered", projection = nameof(OrderSummaryProjection) });
    }

    [HttpPost("projections/multistream/rebuild")]
    public async Task<IActionResult> RebuildMultiStream(CancellationToken ct)
    {
        using var daemon = await _store.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync(typeof(ProductSalesProjection), ct);
        return Ok(new { message = "Multi-stream projection rebuild triggered", projection = nameof(ProductSalesProjection) });
    }

    [HttpGet("projections/status")]
    public IActionResult Status() => Ok(new { daemon = "ready", time = DateTime.UtcNow });
}
