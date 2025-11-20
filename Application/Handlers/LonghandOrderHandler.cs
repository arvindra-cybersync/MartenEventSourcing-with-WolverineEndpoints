using Domain.Entities;
using Domain.Events;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;
using Wolverine.Marten;
using static Domain.Entities.Order;

namespace Application.Handlers
{
    internal class LonghandOrderHandler
    {
        public static async Task Handle(
        CreateOrder command,
        IDocumentSession session,
        IMartenOutbox outbox,
        CancellationToken cancellation)
        {
            var order = new Order
            {
                Description = command.Description
            };

            // Register the new document with Marten
            session.Store(order);

            // Hold on though, this message isn't actually sent
            // until the Marten session is committed
            await outbox.SendAsync(new OrderCreated1(order.Id));

            // This makes the database commits, *then* flushed the
            // previously registered messages to Wolverine's sending
            // agents
            await session.SaveChangesAsync(cancellation);
        }
    }
}
