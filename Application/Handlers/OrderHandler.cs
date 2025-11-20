using Domain.Entities;
using Domain.Events;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.Entities.Order;
using JasperFx.Core;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Marten;
using Wolverine.Attributes;


namespace Application.Handlers
{
    internal class OrderHandler
    {
        // Note that we're able to avoid doing any kind of asynchronous
        // code in this handler

        [Transactional]
        public static OrderCreated1 Handle(CreateOrder command, IDocumentSession session)
        {
            var order = new Order
            {
                Description = command.Description
            };

            // Register the new document with Marten
            session.Store(order);

            // Utilizing Wolverine's "cascading messages" functionality
            // to have this message sent through Wolverine
            return new OrderCreated1(order.Id);
        }
    }
}
