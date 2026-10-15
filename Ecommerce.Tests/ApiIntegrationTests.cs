using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ecommerce.Api.DTOs;

namespace Ecommerce.Tests;

public sealed class ApiIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProductCatalog_IsAvailableWithoutAuthentication()
    {
        var response = await _client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(products.GetArrayLength() >= 5);
        Assert.Contains(products.EnumerateArray(), product =>
            product.GetProperty("name").GetString() == "Steam Deck OLED 512GB");
    }

    [Fact]
    public async Task OrderEndpoint_RequiresAuthentication()
    {
        var response = await _client.PostAsJsonAsync(
            "/orders",
            new OrderCreateReq([new OrderItemReq(1, 1)]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotCreateProducts()
    {
        var token = await LoginAsync("Gabriel", "Pass123@");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/products")
        {
            Content = JsonContent.Create(new ProductDto("Unauthorized console", 99m, 1))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CanCreateOrderAndPayOnlyOnce()
    {
        var token = await LoginAsync("Gabriel", "Pass123@");
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(new OrderCreateReq([new OrderItemReq(1, 1)]))
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal("PendingPayment", order.Status);

        using var paymentRequest = new HttpRequestMessage(HttpMethod.Post, $"/payments/pix/{order.Id}");
        paymentRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var paymentResponse = await _client.SendAsync(paymentRequest);

        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal("Pix", payment.PaymentMethod);
        Assert.Equal("Paid", payment.Status);

        using var repeatedRequest = new HttpRequestMessage(HttpMethod.Post, $"/payments/card/{order.Id}");
        repeatedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var repeatedResponse = await _client.SendAsync(repeatedRequest);

        Assert.Equal(HttpStatusCode.Conflict, repeatedResponse.StatusCode);
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(username, password));
        Assert.True(response.IsSuccessStatusCode, $"Login failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login.Token;
    }
}
