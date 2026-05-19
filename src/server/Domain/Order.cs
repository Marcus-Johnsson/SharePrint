namespace SharePrint.Domain;
public enum OrderStatus { Paid }

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuyerId { get; set; } = "";
    public decimal TotalPrice { get; set; } = 0;
    public string Currency { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Paid;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}