using Infrastructure.ReadModels;
using Marten;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace WebApi.Endpoints
{
    public static class ProductsQueryEndpoints
    {
        // GET /api/products/{id}/sales
        [WolverineGet("/api/products/{id:guid}/sales")]
        public static async Task<ProductSales?> GetSales(Guid id, IQuerySession query)
        {
            return await query.LoadAsync<ProductSales>(id);
        }

        // GET /api/products/top?n=10
        [WolverineGet("/api/products/top")]
        public static async Task<IReadOnlyList<ProductSales>> TopProducts(IQuerySession query, [FromQuery] int n = 10)
        {
            var list = await query.Query<ProductSales>()
                                  .OrderByDescending(x => x.TotalQuantitySold)
                                  .Take(n)
                                  .ToListAsync();

            return list;
        }
    }


}
