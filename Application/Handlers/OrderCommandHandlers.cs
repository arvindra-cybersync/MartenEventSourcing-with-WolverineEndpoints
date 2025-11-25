using Domain.Aggregates;
using Domain.Commands;
using Domain.Constants;
using Domain.Events;
using Marten;
using Microsoft.Extensions.Logging;
using Wolverine.Marten;

namespace Application.Handlers
{
    public class OrderCommandHandlers
    {
        private readonly IDocumentSession _session;
        private readonly IMartenOutbox _outbox;
        private readonly ILogger<OrderCommandHandlers> _logger;

        public OrderCommandHandlers(IDocumentSession session, IMartenOutbox outbox, ILogger<OrderCommandHandlers> logger)
        {
            _session = session;
            _outbox = outbox;
            _logger = logger;
        }

        /// <summary>
        /// Helper method to append events to stream, publish to outbox, and save changes.
        /// Reduces code duplication across all handlers.
        /// </summary>
        private async Task AppendAndPublishEvents(Guid streamId, IEnumerable<object> events, CancellationToken ct)
        {
            var eventList = events.ToList();

            if (!eventList.Any())
            {
                _logger.LogInformation("No events to persist for stream {StreamId}", streamId);
                return;
            }

            foreach (var e in eventList)
            {
                _session.Events.Append(streamId, e);
                _logger.LogDebug("Appended {EventType} to stream {StreamId}", e.GetType().Name, streamId);
            }

            foreach (var e in eventList)
            {
                await _outbox.PublishAsync(e);
            }

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("Persisted {EventCount} event(s) for stream {StreamId}", eventList.Count, streamId);
        }

        /// <summary>
        /// Creates a new order by starting an event stream.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when order already exists</exception>
        public async Task Handle(CreateOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(CreateOrder) - {OrderId}", cmd.OrderId);

            var exists = await _session.Events.FetchStreamStateAsync(cmd.OrderId, ct);
            if (exists != null)
            {
                _logger.LogWarning("Order already exists: {OrderId}", cmd.OrderId);
                throw new InvalidOperationException(ErrorMessages.OrderAlreadyExists);
            }

            var events = OrderAggregate.Create(cmd.OrderId, cmd.CustomerId, cmd.Description, cmd.OccurredAt).ToList();

            // Start the stream with the produced events
            foreach (var e in events)
            {
                _session.Events.StartStream<OrderAggregate>(cmd.OrderId, e);
                _logger.LogDebug("Started stream {OrderId} with event {EventType}", cmd.OrderId, e.GetType().Name);
            }

            // Publish via outbox (transactional with session.SaveChangesAsync)
            foreach (var e in events)
            {
                await _outbox.PublishAsync(e);
            }

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("Order created & saved: {OrderId}", cmd.OrderId);
        }

        /// <summary>
        /// Adds an item to an existing order.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when order is already shipped or cancelled</exception>
        public async Task Handle(AddOrderItemCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(AddOrderItem) - {OrderId}, Item {ItemId}", cmd.OrderId, cmd.ItemId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId, token: ct)
                          ?? throw new KeyNotFoundException(ErrorMessages.OrderNotFound);

            var newEvents = aggregate.AddItem(cmd.ItemId, cmd.ItemName, cmd.Quantity, cmd.OccurredAt);

            await AppendAndPublishEvents(cmd.OrderId, newEvents, ct);
        }

        /// <summary>
        /// Marks an order as shipped.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when order is already shipped or cancelled</exception>
        public async Task Handle(ShipOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(ShipOrder) - {OrderId}", cmd.OrderId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId, token: ct)
                          ?? throw new KeyNotFoundException(ErrorMessages.OrderNotFound);

            var events = aggregate.Ship(cmd.OccurredAt);

            await AppendAndPublishEvents(cmd.OrderId, events, ct);
        }

        /// <summary>
        /// Cancels an order.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when order is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when order is already shipped or cancelled</exception>
        public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(CancelOrder) - {OrderId}", cmd.OrderId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId, token: ct)
                          ?? throw new KeyNotFoundException(ErrorMessages.OrderNotFound);

            var events = aggregate.Cancel(cmd.Reason, cmd.OccurredAt);

            await AppendAndPublishEvents(cmd.OrderId, events, ct);
        }
    }
}
