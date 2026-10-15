using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ecommerce.Api.Services;
using Ecommerce.Api.DTOs;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _service;

        public OrdersController(OrderService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateReq req)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await _service.CreateOrderAsync(userId, req.Items);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(MyOrders), new { id = result.Order!.Id }, result.Order);
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyOrders([FromServices] Ecommerce.Api.Data.AppDbContext db)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var orders = await db.Orders
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();
            return Ok(orders);
        }
    }
}
