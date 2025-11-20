using Marten.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events
{
    // Versioned event types. When evolving, create new types (e.g., OrderItemAddedV2) or
    // maintain converters in projections.
    // Event types used by aggregates, handlers and projections
    // Versionable, append-only events
    public record OrderCreated([property: Identity] Guid OrderId, Guid CustomerId, string Description, DateTime OccurredAt);
    public record OrderItemAdded([property: Identity] Guid OrderId, Guid ItemId, string ItemName, int Quantity, DateTime OccurredAt);
    public record OrderShipped([property: Identity] Guid OrderId, DateTime OccurredAt);
    public record OrderCancelled([property: Identity] Guid OrderId, string Reason, DateTime OccurredAt);

    // Example of versioned event: if you later add currency, create OrderCreatedV2...
    // public record OrderCreatedV2(Guid OrderId, Guid CustomerId, string Description, string Currency, DateTime OccurredAt);
}
