namespace Infrastructure.ReadModels
{
    public class ProductSales
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = "";
        public int TotalQuantitySold { get; set; }
        public DateTime LastSaleAt { get; set; }
    }
}
