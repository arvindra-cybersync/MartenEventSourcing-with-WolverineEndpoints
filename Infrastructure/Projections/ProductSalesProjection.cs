using Domain.Events;
using Infrastructure.ReadModels;
using Marten.Events.Aggregation;
using Marten.Events.Projections;

public class ProductSalesProjection
    : MultiStreamProjection<ProductSales, Guid>
{
    public ProductSalesProjection()
    {
        // Correct slicing key for multi-stream projections (Marten 8)
        Identity<ItemAdded>(e => e.ItemId);
    }

    // Create new document when stream starts
    public ProductSales Create(ItemAdded e)
        => new ProductSales
        {
            Id = e.ItemId,
            ProductName = e.Item,
            TotalQuantitySold = e.Quantity,
            LastSaleAt = e.OccurredAt == default
                ? DateTime.UtcNow
                : e.OccurredAt
        };

    // Update existing stream document
    public void Apply(ItemAdded e, ProductSales view)
    {
        if (string.IsNullOrWhiteSpace(view.ProductName))
            view.ProductName = e.Item;

        view.TotalQuantitySold += e.Quantity;
        view.LastSaleAt = e.OccurredAt == default
            ? DateTime.UtcNow
            : e.OccurredAt;
    }
}
