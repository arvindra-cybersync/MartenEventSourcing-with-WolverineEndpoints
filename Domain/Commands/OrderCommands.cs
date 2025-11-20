using System.Text.Json.Serialization;
using Marten.Schema;

namespace Domain.Commands
{
    public record CreateOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        Guid CustomerId,
        string Description,
        [property: JsonIgnore] DateTime OccurredAt
    );

    public record AddOrderItemCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        Guid ItemId,
        string ItemName,
        int Quantity,
        [property: JsonIgnore] DateTime OccurredAt
    );

    public record ShipOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        [property: JsonIgnore] DateTime OccurredAt
    );

    public record CancelOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        string Reason,
        [property: JsonIgnore] DateTime OccurredAt
    );
}
