namespace Domain.Entities;

using Domain.Events;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Items { get; private set; } = new();
    public bool IsShipped { get; private set; }

    // Default constructor required by Marten
    public Order() { }

    public record CreateOrder(string Description);

    public record OrderCreated1(Guid Id);

    public static Order Create(Guid id, string customerName)
    {
        var order = new Order();
        order.Apply(new OrderCreated { OrderId = id, CustomerName = customerName });
        return order;
    }

    public void AddItem(string item)
    {
        Apply(new ItemAdded { OrderId = Id, Item = item });
    }

    public void Ship()
    {
        if (IsShipped)
            throw new InvalidOperationException("Order already shipped.");

        Apply(new OrderShipped { OrderId = Id, ShippedAt = DateTime.UtcNow });
    }

    // Apply domain events
    private void Apply(object @event)
    {
        When(@event);
    }

    private void When(object @event)
    {
        switch (@event)
        {
            case OrderCreated e:
                Id = e.OrderId;
                CustomerName = e.CustomerName;
                break;

            case ItemAdded e:
                Items.Add(e.Item);
                break;

            case OrderShipped e:
                IsShipped = true;
                break;
        }
    }
}
