using Infrastructure.ReadModels;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IQuerySession _query;

    public OrdersController(IQuerySession query) => _query = query;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var summary = await _query.LoadAsync<OrderSummary>(id);
        return summary is null ? NotFound() : Ok(summary);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id)
    {
        var timeline = await _query.Query<Infrastructure.ReadModels.OrderTimelineEntry>()
            .Where(x => x.OrderId == id)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();

        return Ok(timeline);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var summaries = await _query.Query<OrderSummary>().ToListAsync();
        return Ok(summaries);
    }
}
