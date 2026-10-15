using System.Security.Claims;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("payments")]
[Authorize]
public class PaymentsController(OrderService orderService) : ControllerBase
{
    [HttpPost("pix/{orderId:int}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> PayWithPix(int orderId) => ConfirmPayment(orderId, "Pix");

    [HttpPost("card/{orderId:int}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> PayWithCard(int orderId) => ConfirmPayment(orderId, "Card");

    private async Task<IActionResult> ConfirmPayment(int orderId, string paymentMethod)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Client";
        var result = await orderService.ConfirmPaymentAsync(orderId, userId, role, paymentMethod);
        if (!result.Success)
            return Problem(title: "Unable to confirm payment", detail: result.Message, statusCode: result.StatusCode);

        var order = result.Order!;
        return Ok(new PaymentResponse(order.Id, order.Status, order.PaymentMethod, order.PaidAt));
    }
}
