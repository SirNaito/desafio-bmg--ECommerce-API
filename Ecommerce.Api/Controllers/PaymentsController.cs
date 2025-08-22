using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ecommerce.Api.Services;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly OrderService _orderService;

        public PaymentsController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("pix/{orderId}")]
        public async Task<IActionResult> PayPix(int orderId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Client";
            var result = await _orderService.ConfirmPaymentAsync(orderId, userId, role);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Order);
        }

        [HttpPost("card/{orderId}")]
        public async Task<IActionResult> PayCard(int orderId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Client";
            var result = await _orderService.ConfirmPaymentAsync(orderId, userId, role);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Order);
        }
    }
}
