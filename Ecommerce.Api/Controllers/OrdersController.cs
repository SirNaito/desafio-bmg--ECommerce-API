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
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.CreateOrderAsync(userId, req.Items);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Order);
        }

        [HttpGet("my")]
        public IActionResult MyOrders([FromServices] Ecommerce.Api.Data.AppDbContext db)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var orders = db.Orders.Where(o => o.UserId == userId).ToList();
            return Ok(orders);
        }
    }
}
