using Domain.Entities;
using Domain.Events;
using JasperFx.Core;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine.Attributes;
using Wolverine;
using Wolverine.Marten;
using static Domain.Entities.Order;

namespace Application.Handlers
{
    internal class OrderHandler2
    {
        [Transactional]
        public static ValueTask Handle(
       CreateOrder command,
       IDocumentSession session,
       IMessageBus bus)
        {
            var order = new Order
            {
                Description = command.Description
            };

            // Register the new document with Marten
            session.Store(order);

            // Utilizing Wolverine's "cascading messages" functionality
            // to have this message sent through Wolverine
            return bus.SendAsync(
                new OrderCreated1(order.Id),
                new DeliveryOptions { DeliverWithin = 5.Minutes() });
        }
    }
}
