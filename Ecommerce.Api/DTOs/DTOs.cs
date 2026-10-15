using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.DTOs;

public sealed record LoginRequest(
    [param: Required, StringLength(50, MinimumLength = 3)] string Username,
    [param: Required, StringLength(100, MinimumLength = 1)] string Password);

public sealed record LoginResponse(string Token);

public sealed record ProductDto(
    [param: Required, StringLength(120, MinimumLength = 2)] string Name,
    [param: Range(typeof(decimal), "0.01", "1000000000")] decimal Price,
    [param: Range(0, 1000000)] int Stock);

public sealed record ProductResponse(int Id, string Name, decimal Price, int Stock);

public sealed record OrderItemReq(
    [param: Range(1, int.MaxValue)] int ProductId,
    [param: Range(1, 1000)] int Quantity);

public sealed record OrderCreateReq(
    [param: Required, MinLength(1)] List<OrderItemReq> Items);

public sealed record OrderItemResponse(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderResponse(
    int Id,
    int UserId,
    decimal Total,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? PaymentMethod,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record PaymentResponse(
    int OrderId,
    string Status,
    string? PaymentMethod,
    DateTime? PaidAt);
