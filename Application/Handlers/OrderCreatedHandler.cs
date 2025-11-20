using Domain.Events;

namespace Application.Handlers
{
    internal class OrderCreatedHandler
    {
        public static void Handle(OrderCreated created)
        {
            Console.WriteLine($"I got an OrderCreated {created.OrderId}");
        }
    }
}
