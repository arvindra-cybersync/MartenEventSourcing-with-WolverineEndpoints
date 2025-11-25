using Infrastructure.ReadModels;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Configuration;
using Wolverine.Http;

namespace WebApi.Endpoints
{
    public static class ProductsQueryEndpoints
    {
        // GET /api/products/{id}/sales
        [WolverineGet("/api/products/{id:guid}/sales")]
        [Authorize(Policy = AuthorizationPolicies.OrderRead)]
        public static async Task<ProductSales?> GetSales(Guid id, IQuerySession query)
        {
            return await query.LoadAsync<ProductSales>(id);
        }

        // GET /api/products/top?n=10
        [WolverineGet("/api/products/top")]
        [Authorize(Policy = AuthorizationPolicies.OrderRead)]
        public static async Task<IReadOnlyList<ProductSales>> TopProducts(IQuerySession query, [FromQuery] int n = 10)
        {
            // Validate and constrain input to prevent abuse
            n = Math.Clamp(n, 1, 100);

            var list = await query.Query<ProductSales>()
                                  .OrderByDescending(x => x.TotalQuantitySold)
                                  .Take(n)
                                  .ToListAsync();

            return list;
        }
    }


}
