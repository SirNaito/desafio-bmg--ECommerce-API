using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Services
{
    public class OrderService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OrderService> _logger;

        public OrderService(AppDbContext db, ILogger<OrderService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(int userId, List<OrderItemReq> items)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (items == null || items.Count == 0)
                    return (false, "Carrinho vazio", null);

                var order = new Order { UserId = userId, Status = "PendingPayment" };
                decimal total = 0m;

                foreach (var it in items)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
                    if (product == null) return (false, $"Produto {it.ProductId} não encontrado", null);
                    if (it.Quantity <= 0) return (false, "Quantidade inválida", null);
                    if (product.Stock < it.Quantity) return (false, $"Estoque insuficiente para {product.Name}", null);

                    product.Stock -= it.Quantity;
                    order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = it.Quantity, UnitPrice = product.Price });
                    total += product.Price * it.Quantity;
                }

                order.Total = total;
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Pedido {OrderId} criado para usuário {UserId}", order.Id, userId);
                return (true, "Pedido criado", order);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar pedido para user {UserId}", userId);
                return (false, "Erro interno", null);
            }
        }

        public async Task<(bool Success, string Message, Order? Order)> ConfirmPaymentAsync(int orderId, int userId, string role)
        {
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return (false, "Pedido não encontrado", null);

            if (role != "Admin" && order.UserId != userId)
                return (false, "Não autorizado", null);

            order.Status = "Paid";
            order.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Pagamento confirmado para pedido {OrderId}", orderId);
            return (true, "Pagamento confirmado", order);
        }
    }
}
