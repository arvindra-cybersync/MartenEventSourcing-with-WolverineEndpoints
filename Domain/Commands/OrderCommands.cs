using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Marten.Schema;

namespace Domain.Commands
{
    /// <summary>
    /// Command to create a new order.
    /// </summary>
    public record CreateOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        Guid CustomerId,
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")]
        string Description,
        [property: JsonIgnore] DateTime OccurredAt
    );

    /// <summary>
    /// Command to add an item to an existing order.
    /// </summary>
    public record AddOrderItemCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        Guid ItemId,
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 200 characters")]
        string ItemName,
        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
        int Quantity,
        [property: JsonIgnore] DateTime OccurredAt
    );

    /// <summary>
    /// Command to mark an order as shipped.
    /// </summary>
    public record ShipOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        [property: JsonIgnore] DateTime OccurredAt
    );

    /// <summary>
    /// Command to cancel an order.
    /// </summary>
    public record CancelOrderCommand(
        [property: JsonIgnore, Identity] Guid OrderId,
        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Reason must be between 1 and 500 characters")]
        string Reason,
        [property: JsonIgnore] DateTime OccurredAt
    );
}
