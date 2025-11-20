using Domain.Events;

namespace Domain.Aggregates
{
    /// <summary>
    /// Event-sourced aggregate for Orders.
    /// Version property mirrors the number of applied events (optional for projections/debugging).
    /// </summary>
    public class OrderAggregate
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public bool IsShipped { get; private set; }
        public bool IsCancelled { get; private set; }

        private readonly Dictionary<Guid, (string Name, int Quantity)> _items = new();
        public IReadOnlyDictionary<Guid, (string Name, int Quantity)> Items => _items;

        // Safe mirrored version for debugging/projections
        public int Version { get; private set; } = 0;

        public OrderAggregate() { }

        // -------------------
        // Event Appliers
        // -------------------
        public void Apply(OrderCreated e)
        {
            Id = e.OrderId;
            CustomerId = e.CustomerId;
            Description = e.Description;
            Version++;
        }

        public void Apply(OrderItemAdded e)
        {
            if (_items.TryGetValue(e.ItemId, out var existing))
                _items[e.ItemId] = (existing.Name, existing.Quantity + e.Quantity);
            else
                _items[e.ItemId] = (e.ItemName, e.Quantity);

            Version++;
        }

        public void Apply(OrderShipped e)
        {
            IsShipped = true;
            Version++;
        }

        public void Apply(OrderCancelled e)
        {
            IsCancelled = true;
            Version++;
        }

        // -------------------
        // Behavior Methods (produce events)
        // -------------------
        public static IEnumerable<object> Create(Guid orderId, Guid customerId, string description, DateTime occurredAt)
        {
            yield return new OrderCreated(orderId, customerId, description, occurredAt);
        }

        public IEnumerable<object> AddItem(Guid itemId, string itemName, int quantity, DateTime occurredAt)
        {
            if (IsShipped) throw new InvalidOperationException("Order already shipped");
            if (IsCancelled) throw new InvalidOperationException("Order cancelled");
            if (quantity <= 0) throw new ArgumentException("Quantity must be > 0");

            yield return new OrderItemAdded(Id, itemId, itemName, quantity, occurredAt);
        }

        public IEnumerable<object> Ship(DateTime occurredAt)
        {
            if (IsShipped) throw new InvalidOperationException("Order already shipped");
            if (IsCancelled) throw new InvalidOperationException("Order cancelled");

            yield return new OrderShipped(Id, occurredAt);
        }

        public IEnumerable<object> Cancel(string reason, DateTime occurredAt)
        {
            if (IsShipped) throw new InvalidOperationException("Can't cancel shipped order");
            if (IsCancelled) throw new InvalidOperationException("Already cancelled");

            yield return new OrderCancelled(Id, reason, occurredAt);
        }
    }
}
