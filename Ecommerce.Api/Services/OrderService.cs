using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Http;
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

    public async Task<(bool Success, string Message, Order? Order, int StatusCode)> CreateOrderAsync(
        int userId, List<OrderItemReq>? items)
    {
        if (userId <= 0)
            return (false, "The authenticated user is invalid.", null, StatusCodes.Status401Unauthorized);
        if (items is null || items.Count == 0)
            return (false, "The cart cannot be empty.", null, StatusCodes.Status400BadRequest);
        if (items.Any(item => item.ProductId <= 0 || item.Quantity <= 0 || item.Quantity > 1000))
            return (false, "Each item must have a valid product ID and a quantity between 1 and 1000.",
                null, StatusCodes.Status400BadRequest);

        var requestedItems = items
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => (long)item.Quantity) })
            .ToList();

        if (requestedItems.Any(item => item.Quantity > 1000))
            return (false, "The total quantity per product cannot exceed 1000.",
                null, StatusCodes.Status400BadRequest);

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var productIds = requestedItems.Select(item => item.ProductId).ToArray();
            var products = await _db.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);

            var missing = requestedItems.FirstOrDefault(item => !products.ContainsKey(item.ProductId));
            if (missing is not null)
                return (false, $"Product {missing.ProductId} was not found.",
                    null, StatusCodes.Status404NotFound);

            foreach (var item in requestedItems)
            {
                var product = products[item.ProductId];
                if (product.Stock < item.Quantity)
                    return (false, $"There is not enough stock for '{product.Name}'.",
                        null, StatusCodes.Status409Conflict);
            }

            var order = new Order { UserId = userId };
            foreach (var item in requestedItems)
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
            return (true, "Order created successfully.", order, StatusCodes.Status201Created);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create an order for user {UserId}.", userId);
            return (false, "An unexpected error occurred while creating the order.",
                null, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<(bool Success, string Message, Order? Order, int StatusCode)> ConfirmPaymentAsync(
        int orderId, int userId, string role, string paymentMethod)
    {
        var normalizedMethod = paymentMethod?.Trim();
        if (!string.Equals(normalizedMethod, "Pix", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedMethod, "Card", StringComparison.OrdinalIgnoreCase))
            return (false, "Unsupported payment method.", null, StatusCodes.Status400BadRequest);

        normalizedMethod = string.Equals(normalizedMethod, "Pix", StringComparison.OrdinalIgnoreCase) ? "Pix" : "Card";

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var order = await _db.Orders
                .FirstOrDefaultAsync(candidate => candidate.Id == orderId);

            if (order is null)
                return (false, "Order was not found.", null, StatusCodes.Status404NotFound);
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) && order.UserId != userId)
                return (false, "You are not allowed to pay for this order.", null, StatusCodes.Status403Forbidden);
            if (!string.Equals(order.Status, OrderStatuses.PendingPayment, StringComparison.Ordinal))
                return (false, "Only orders awaiting payment can be paid.", null, StatusCodes.Status409Conflict);

            order.Status = OrderStatuses.Paid;
            order.PaymentMethod = normalizedMethod;
            order.PaidAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Simulated {PaymentMethod} payment for order {OrderId}.", normalizedMethod, orderId);
            return (true, "Payment simulated successfully.", order, StatusCodes.Status200OK);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to confirm payment for order {OrderId}.", orderId);
            return (false, "An unexpected error occurred while confirming payment.",
                null, StatusCodes.Status500InternalServerError);
        }
    }
}
