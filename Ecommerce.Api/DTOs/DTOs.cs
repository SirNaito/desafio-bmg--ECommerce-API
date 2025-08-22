namespace Ecommerce.Api.DTOs
{
    public record LoginRequest(string Username, string Password);
    public record ProductDto(string Name, decimal Price, int Stock);
    public record OrderItemReq(int ProductId, int Quantity);
    public record OrderCreateReq(List<OrderItemReq> Items);
    public record CardReq(string CardNumber, string Holder, string Exp, string Cvv);
}
