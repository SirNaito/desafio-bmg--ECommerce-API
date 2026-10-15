using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController(OrderService orderService, AppDbContext db) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] OrderCreateReq request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await orderService.CreateOrderAsync(userId, request.Items);
        if (!result.Success)
            return Problem(title: "Unable to create order", detail: result.Message, statusCode: result.StatusCode);

        var order = result.Order!;
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
    }

    [HttpGet("my")]
    [ProducesResponseType<IReadOnlyCollection<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> MyOrders()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var orders = await db.Orders.AsNoTracking()
            .Include(order => order.Items).ThenInclude(item => item.Product)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var order = await db.Orders.AsNoTracking()
            .Include(candidate => candidate.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);

        if (order is null)
            return NotFound();
        if (!IsAdmin() && order.UserId != userId)
            return Forbid();

        return Ok(ToResponse(order));
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private bool IsAdmin() =>
        string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

    private static OrderResponse ToResponse(Order order) =>
        new(order.Id, order.UserId, order.Total, order.Status, order.CreatedAt, order.PaidAt,
            order.PaymentMethod,
            order.Items.Select(item => new OrderItemResponse(
                item.ProductId, item.Product?.Name ?? $"Product #{item.ProductId}",
                item.Quantity, item.UnitPrice, item.UnitPrice * item.Quantity)).ToArray());
}
