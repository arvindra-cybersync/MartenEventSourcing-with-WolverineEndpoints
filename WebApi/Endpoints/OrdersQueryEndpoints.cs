using Infrastructure.ReadModels;
using Marten;
using Microsoft.AspNetCore.Authorization;
using WebApi.Configuration;
using Wolverine.Http;

namespace WebApi.Endpoints
{
    public static class OrdersQueryEndpoints
    {
        // GET /api/orders/{id}
        [WolverineGet("/api/orders/{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.OrderRead)]
        public static async Task<OrderSummary?> GetOrder(Guid id, IQuerySession query)
        {
            return await query.LoadAsync<OrderSummary>(id);
        }

        // GET /api/orders/{id}/timeline
        [WolverineGet("/api/orders/{id:guid}/timeline")]
        [Authorize(Policy = AuthorizationPolicies.OrderRead)]
        public static async Task<IReadOnlyList<OrderTimelineEntry>> GetTimeline(Guid id, IQuerySession query)
        {
            var timeline = await query.Query<OrderTimelineEntry>()
                .Where(x => x.OrderId == id)
                .OrderBy(x => x.OccurredAt)
                .ToListAsync();

            return timeline;
        }

        // GET /api/orders
        [WolverineGet("/api/orders")]
        [Authorize(Policy = AuthorizationPolicies.OrderRead)]
        public static async Task<IReadOnlyList<OrderSummary>> ListOrders(IQuerySession query)
        {
            var summaries = await query.Query<OrderSummary>().ToListAsync();
            return summaries;
        }
    }
}
