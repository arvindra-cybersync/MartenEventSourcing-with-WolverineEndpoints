using Domain.Commands;
using Wolverine.Http;
using Wolverine;

namespace WebApi.Endpoints
{
    public static class OrdersCommandEndpoints
    {
        // ---------------------------
        // CREATE ORDER
        // ---------------------------
        [WolverinePost("/api/orders")]
        public static async Task<IResult> CreateOrder(CreateOrderCommand cmd, IMessageBus bus)
        {
            try
            {
                var orderId = Guid.NewGuid();

                await bus.InvokeAsync(
                    cmd with
                    {
                        OrderId = orderId,
                        OccurredAt = DateTime.UtcNow
                    });

                return Results.Created($"/api/orders/{orderId}", new
                {
                    message = "Order created successfully",
                    orderId
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        // ---------------------------
        // ADD ITEM
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/items")]
        public static async Task<IResult> AddItem(Guid orderId, AddOrderItemCommand cmd, IMessageBus bus)
        {
            try
            {
                await bus.InvokeAsync(
                    cmd with
                    {
                        OrderId = orderId,
                        OccurredAt = DateTime.UtcNow
                    });

                return Results.Ok(new
                {
                    message = "Item added successfully",
                    orderId,
                    itemId = cmd.ItemId,
                    quantity = cmd.Quantity
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        // ---------------------------
        // SHIP ORDER
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/ship")]
        public static async Task<IResult> ShipOrder(Guid orderId, ShipOrderCommand cmd, IMessageBus bus)
        {
            try
            {
                await bus.InvokeAsync(
                    cmd with
                    {
                        OrderId = orderId,
                        OccurredAt = DateTime.UtcNow
                    });

                return Results.Ok(new
                {
                    message = "Order shipped successfully",
                    orderId
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        // ---------------------------
        // CANCEL ORDER
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/cancel")]
        public static async Task<IResult> CancelOrder(Guid orderId, CancelOrderCommand cmd, IMessageBus bus)
        {
            try
            {
                await bus.InvokeAsync(
                    cmd with
                    {
                        OrderId = orderId,
                        OccurredAt = DateTime.UtcNow
                    });

                return Results.Ok(new
                {
                    message = "Order cancelled successfully",
                    orderId
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }
    }
}
