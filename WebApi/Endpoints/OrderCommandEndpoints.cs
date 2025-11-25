using Domain.Commands;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using WebApi.Configuration;
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
        [Authorize(Policy = AuthorizationPolicies.OrderWrite)]
        public static async Task<IResult> CreateOrder(
            CreateOrderCommand cmd,
            IMessageBus bus,
            ILogger<IResult> logger)
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                logger.LogWarning(ex, "Attempted to create duplicate order");
                return Results.Conflict(new { error = ErrorMessages.OrderAlreadyExists });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error creating order");
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating order");
                return Results.Problem(
                    detail: ErrorMessages.UnexpectedError,
                    statusCode: 500);
            }
        }

        // ---------------------------
        // ADD ITEM
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/items")]
        [Authorize(Policy = AuthorizationPolicies.OrderWrite)]
        public static async Task<IResult> AddItem(
            Guid orderId,
            AddOrderItemCommand cmd,
            IMessageBus bus,
            ILogger<IResult> logger)
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
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return Results.NotFound(new { error = ErrorMessages.OrderNotFound });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot add item to order {OrderId}", orderId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error adding item to order {OrderId}", orderId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error adding item to order {OrderId}", orderId);
                return Results.Problem(
                    detail: ErrorMessages.UnexpectedError,
                    statusCode: 500);
            }
        }

        // ---------------------------
        // SHIP ORDER
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/ship")]
        [Authorize(Policy = AuthorizationPolicies.OrderWrite)]
        public static async Task<IResult> ShipOrder(
            Guid orderId,
            ShipOrderCommand cmd,
            IMessageBus bus,
            ILogger<IResult> logger)
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
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return Results.NotFound(new { error = ErrorMessages.OrderNotFound });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot ship order {OrderId}", orderId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error shipping order {OrderId}", orderId);
                return Results.Problem(
                    detail: ErrorMessages.UnexpectedError,
                    statusCode: 500);
            }
        }

        // ---------------------------
        // CANCEL ORDER
        // ---------------------------
        [WolverinePost("/api/orders/{orderId:guid}/cancel")]
        [Authorize(Policy = AuthorizationPolicies.OrderWrite)]
        public static async Task<IResult> CancelOrder(
            Guid orderId,
            CancelOrderCommand cmd,
            IMessageBus bus,
            ILogger<IResult> logger)
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
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return Results.NotFound(new { error = ErrorMessages.OrderNotFound });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot cancel order {OrderId}", orderId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error cancelling order {OrderId}", orderId);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error cancelling order {OrderId}", orderId);
                return Results.Problem(
                    detail: ErrorMessages.UnexpectedError,
                    statusCode: 500);
            }
        }
    }
}
