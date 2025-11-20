using Domain.Events;
using Infrastructure.ReadModels;
using Marten.Events.Projections;

namespace Infrastructure.Projections
{
    public class OrderTimelineProjection : EventProjection
    {
        public OrderTimelineEntry Create(OrderCreated e)
            => new OrderTimelineEntry { OrderId = e.OrderId, EventType = nameof(OrderCreated), Payload = e, OccurredAt = e.OccurredAt };

        public OrderTimelineEntry Create(OrderItemAdded e)
            => new OrderTimelineEntry { OrderId = e.OrderId, EventType = nameof(OrderItemAdded), Payload = e, OccurredAt = e.OccurredAt };

        public OrderTimelineEntry Create(OrderShipped e)
            => new OrderTimelineEntry { OrderId = e.OrderId, EventType = nameof(OrderShipped), Payload = e, OccurredAt = e.OccurredAt };

        public OrderTimelineEntry Create(OrderCancelled e)
            => new OrderTimelineEntry { OrderId = e.OrderId, EventType = nameof(OrderCancelled), Payload = e, OccurredAt = e.OccurredAt };
    }
}
