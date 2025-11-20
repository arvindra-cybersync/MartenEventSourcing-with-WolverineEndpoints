using Domain.Aggregates;
using Domain.Commands;
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

        // CREATE
        // This handler starts a new event stream for the order
        public async Task Handle(CreateOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(CreateOrder) - {OrderId}", cmd.OrderId);

            var exists = await _session.Events.FetchStreamStateAsync(cmd.OrderId, ct);
            if (exists != null)
            {
                _logger.LogWarning("Order already exists: {OrderId}", cmd.OrderId);
                throw new InvalidOperationException("Order already exists");
            }

            var events = OrderAggregate.Create(cmd.OrderId, cmd.CustomerId, cmd.Description, cmd.OccurredAt).ToList();

            // Start the stream with the produced events
            foreach (var e in events)
            {
                _session.Events.StartStream<OrderAggregate>(cmd.OrderId, e);
                _logger.LogDebug("Started stream {OrderId} event {EventType}", cmd.OrderId, e.GetType().Name);
            }

            // Publish via outbox (transactional with session.SaveChangesAsync)
            foreach (var e in events)
            {
                await _outbox.PublishAsync(e);
            }

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("Order created & saved: {OrderId}", cmd.OrderId);
        }

        // ADD ITEM
        public async Task Handle(AddOrderItemCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(AddOrderItem) - {OrderId}, Item {ItemId}", cmd.OrderId, cmd.ItemId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId)
                          ?? throw new KeyNotFoundException($"Order not found: {cmd.OrderId}");

            var newEvents = aggregate.AddItem(cmd.ItemId, cmd.ItemName, cmd.Quantity, cmd.OccurredAt).ToList();

            if (!newEvents.Any())
            {
                _logger.LogInformation("No events produced for AddItem on {OrderId}", cmd.OrderId);
                return;
            }

            foreach (var e in newEvents)
            {
                _session.Events.Append(cmd.OrderId, e);
                _logger.LogDebug("Appended event {EventType} to stream {OrderId}", e.GetType().Name, cmd.OrderId);
            }

            foreach (var e in newEvents)
                await _outbox.PublishAsync(e);

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("AddItem persisted for {OrderId}", cmd.OrderId);
        }

        // SHIP
        public async Task Handle(ShipOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(ShipOrder) - {OrderId}", cmd.OrderId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId)
                          ?? throw new KeyNotFoundException($"Order not found: {cmd.OrderId}");

            var events = aggregate.Ship(cmd.OccurredAt).ToList();

            foreach (var e in events)
            {
                _session.Events.Append(cmd.OrderId, e);
                _logger.LogDebug("Appended {EventType}", e.GetType().Name);
            }

            foreach (var e in events)
                await _outbox.PublishAsync(e);

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("Ship event persisted for {OrderId}", cmd.OrderId);
        }

        // CANCEL
        public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Handle(CancelOrder) - {OrderId}", cmd.OrderId);

            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(cmd.OrderId)
                          ?? throw new KeyNotFoundException($"Order not found: {cmd.OrderId}");

            var events = aggregate.Cancel(cmd.Reason, cmd.OccurredAt).ToList();

            foreach (var e in events)
            {
                _session.Events.Append(cmd.OrderId, e);
                _logger.LogDebug("Appended {EventType}", e.GetType().Name);
            }

            foreach (var e in events)
                await _outbox.PublishAsync(e);

            await _session.SaveChangesAsync(ct);
            _logger.LogInformation("Cancel event persisted for {OrderId}", cmd.OrderId);
        }
    }
}
