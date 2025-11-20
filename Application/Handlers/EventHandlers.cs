using Domain.Events;

namespace Application.Handlers
{
    public static class EventHandlers
    {
        public static Task Handle(OrderItemAdded ev)
        {
            Console.WriteLine($"Item added: {ev.ItemName} x{ev.Quantity} on order {ev.OrderId}");
            return Task.CompletedTask;
        }

        public static Task Handle(OrderShipped ev)
        {
            Console.WriteLine($"Order shipped: {ev.OrderId} at {ev.OccurredAt}");
            return Task.CompletedTask;
        }
    }
}
