using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(
        int userId, List<OrderItemReq> items)
    {
        if (userId <= 0)
            return (false, "Invalid authenticated user", null);
        if (items is null || items.Count == 0)
            return (false, "Cart cannot be empty", null);
        if (items.Any(item => item.ProductId <= 0 || item.Quantity <= 0 || item.Quantity > 1000))
            return (false, "Each item must have a valid product ID and quantity between 1 and 1000", null);

        var requested = items.GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => (long)item.Quantity) })
            .ToList();
        if (requested.Any(item => item.Quantity > 1000))
            return (false, "Total quantity per product cannot exceed 1000", null);

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var productIds = requested.Select(item => item.ProductId).ToArray();
            var products = await _db.Products.Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);

            var missing = requested.FirstOrDefault(item => !products.ContainsKey(item.ProductId));
            if (missing is not null)
                return (false, "Product " + missing.ProductId + " was not found", null);

            foreach (var item in requested)
            {
                var product = products[item.ProductId];
                if (product.Stock < item.Quantity)
                    return (false, "Insufficient stock for " + product.Name, null);
            }

            var order = new Order { UserId = userId };
            foreach (var item in requested)
            {
                var product = products[item.ProductId];
                var quantity = (int)item.Quantity;
                product.Stock -= quantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
                order.Total += product.Price * quantity;
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} created for user {UserId}.", order.Id, userId);
            return (true, "Order created successfully", order);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create an order for user {UserId}.", userId);
            return (false, "An unexpected error occurred while creating the order", null);
        }
    }

    public async Task<(bool Success, string Message, Order? Order)> ConfirmPaymentAsync(
        int orderId, int userId, string role, string paymentMethod)
    {
        if (paymentMethod is not ("Pix" or "Card"))
            return (false, "Unsupported payment method", null);

        var order = await _db.Orders.Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId);
        if (order is null)
            return (false, "Order was not found", null);
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && order.UserId != userId)
            return (false, "Not authorized to pay for this order", null);
        if (order.Status != "PendingPayment")
            return (false, "Only orders awaiting payment can be paid", null);

        order.Status = "Paid";
        order.PaymentMethod = paymentMethod;
        order.PaidAt = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Simulated {PaymentMethod} payment for order {OrderId}.", paymentMethod, orderId);
            return (true, "Payment simulated successfully", order);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to confirm payment for order {OrderId}.", orderId);
            return (false, "An unexpected error occurred while confirming payment", null);
        }
    }
}
