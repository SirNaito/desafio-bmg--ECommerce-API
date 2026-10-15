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

        [HttpPost("pix/{orderId:int}")]
        public Task<IActionResult> PayPix(int orderId) => ConfirmPayment(orderId, "Pix");

        [HttpPost("card/{orderId:int}")]
        public Task<IActionResult> PayCard(int orderId) => ConfirmPayment(orderId, "Card");

        private async Task<IActionResult> ConfirmPayment(int orderId, string paymentMethod)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Client";
            var result = await _orderService.ConfirmPaymentAsync(orderId, userId, role, paymentMethod);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Order);
        }
    }
}
