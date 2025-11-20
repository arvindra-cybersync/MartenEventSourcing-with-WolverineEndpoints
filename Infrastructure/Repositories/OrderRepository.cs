using Marten;
using Domain.Aggregates;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Repository that uses Marten event store as the source of truth.
    /// It supports snapshot read/write to accelerate rehydration.
    /// </summary>
    public class OrderRepository
    {
        private readonly IDocumentSession _session;
        private readonly int _snapshotThreshold = 50; // events before a snapshot

        public OrderRepository(IDocumentSession session)
        {
            _session = session;
        }

        public async Task<OrderAggregate> LoadAsync(Guid orderId, CancellationToken ct = default)
        {
            // Marten can reconstruct with snapshot support if you configure aggregate typing.
            var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(orderId);
            if (aggregate is null)
                throw new KeyNotFoundException($"Order {orderId} not found");
            return aggregate;
        }

        public async Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default)
        {
            var stream = await _session.Events.FetchStreamStateAsync(orderId, ct);
            return stream != null;
        }

        /// <summary>Append events to the stream (transactional with session.SaveChangesAsync).</summary>
        public void AppendEvents(Guid orderId, IEnumerable<object> events)
        {
            foreach (var e in events)
                _session.Events.Append(orderId, e);
        }

        /// <summary>Optional: create snapshot document for faster rebuilds.</summary>
        public async Task MaybeCreateSnapshotAsync(Guid orderId, CancellationToken ct = default)
        {
            var stream = await _session.Events.FetchStreamStateAsync(orderId, ct);
            if (stream == null) return;

            if (stream.Version > 0 && (stream.Version % _snapshotThreshold) == 0)
            {
                var aggregate = await _session.Events.AggregateStreamAsync<OrderAggregate>(orderId);
                // store snapshot as a document type named OrderSnapshot (not implemented above yet)
                _session.Store(new OrderSnapshot { Id = orderId, Aggregate = aggregate!, Version = aggregate!.Version, SnapshotAt = DateTime.UtcNow });
            }
        }
    }

    public class OrderSnapshot
    {
        public Guid Id { get; set; }
        public OrderAggregate Aggregate { get; set; } = default!;
        public int Version { get; set; }
        public DateTime SnapshotAt { get; set; }
    }
}
