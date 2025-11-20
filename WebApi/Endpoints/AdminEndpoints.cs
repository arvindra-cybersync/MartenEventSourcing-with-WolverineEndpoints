using Domain.Commands;
using Infrastructure.Projections;
using Infrastructure.ReadModels;
using Infrastructure.Repositories;
using Marten;
using Wolverine;
using Wolverine.Http;

namespace WebApi.Endpoints
{
    public static class AdminEndpoints
    {
        // Rebuild single-stream projection
        [WolverinePost("/api/admin/projections/rebuild")]
        public static async Task<object> RebuildProjections(IDocumentStore store, CancellationToken ct)
        {
            // BuildProjectionDaemonAsync returns an IProjectionDaemon (disposable, not IAsyncDisposable)
            var daemon = await store.BuildProjectionDaemonAsync();
            using (daemon)
            {
                await daemon.RebuildProjectionAsync(typeof(OrderSummaryProjection), ct);
            }

            return new { message = "Projection rebuild triggered", projection = nameof(OrderSummaryProjection) };
        }

        // Rebuild multi-stream projection
        [WolverinePost("/api/admin/projections/multistream/rebuild")]
        public static async Task<object> RebuildMultiStreamProjections(IDocumentStore store, CancellationToken ct)
        {
            var daemon = await store.BuildProjectionDaemonAsync();
            using (daemon)
            {
                await daemon.RebuildProjectionAsync(typeof(ProductSalesProjection), ct);
            }

            return new { message = "Multi-stream projection rebuild triggered", projection = nameof(ProductSalesProjection) };
        }

        // Simple health/status endpoint
        [WolverineGet("/api/admin/projections/status")]
        public static object Status() => new { daemon = "ready", time = DateTime.UtcNow };
    }
}
