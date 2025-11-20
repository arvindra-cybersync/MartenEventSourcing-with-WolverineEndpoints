namespace Infrastructure.ReadModels;

public class OrderSummary
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Description { get; set; } = "";
    public int TotalItems { get; set; }
    public bool IsShipped { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
