namespace Ecommerce.Api.Models;

public static class OrderStatuses
{
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Status { get; set; } = OrderStatuses.PendingPayment;
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
