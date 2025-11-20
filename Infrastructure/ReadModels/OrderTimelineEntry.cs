namespace Infrastructure.ReadModels
{
    public class OrderTimelineEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string EventType { get; set; } = "";
        public object Payload { get; set; } = new { };
        public DateTime OccurredAt { get; set; }
    }
}
