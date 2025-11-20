using Domain.Events;
using Infrastructure.ReadModels;
using Marten.Events.Aggregation;
using System;

namespace Infrastructure.Projections
{
    // TDoc = OrderSummary, TId = Guid
    public class OrderSummaryProjection : SingleStreamProjection<OrderSummary, Guid>
    {
        public OrderSummaryProjection()
        {
            // Optional: nothing needed here unless you want metadata 
        }
        // Create initial document when OrderCreated is seen
        public OrderSummary Create(OrderCreated e) => new OrderSummary
        {
            Id = e.OrderId,
            CustomerId = e.CustomerId,
            Description = e.Description,
            TotalItems = 0,
            IsShipped = false,
            IsCancelled = false,
            UpdatedAt = e.OccurredAt
        };

        // Apply item additions
        public void Apply(OrderItemAdded e, OrderSummary doc)
        {
            doc.TotalItems += e.Quantity;
            doc.UpdatedAt = e.OccurredAt;
        }

        // Apply ship
        public void Apply(OrderShipped e, OrderSummary doc)
        {
            doc.IsShipped = true;
            doc.UpdatedAt = e.OccurredAt;
        }

        // Apply cancellation
        public void Apply(OrderCancelled e, OrderSummary doc)
        {
            doc.IsCancelled = true;
            doc.UpdatedAt = e.OccurredAt;
        }
    }
}
