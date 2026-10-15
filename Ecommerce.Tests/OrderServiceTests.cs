using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecommerce.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_CreatesOrderAndReservesStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(database.CustomerId, [new OrderItemReq(1, 2)]);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Order);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(20m, result.Order.Total);
        Assert.Equal(OrderStatuses.PendingPayment, result.Order.Status);
        Assert.Equal(3, await database.Context.Products.Where(p => p.Id == 1).Select(p => p.Stock).SingleAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsEmptyCart()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(database.CustomerId, []);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(0, await database.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_LeavesStockUnchangedWhenInsufficient()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(database.CustomerId, [new OrderItemReq(2, 2)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(1, await database.Context.Products.Where(p => p.Id == 2).Select(p => p.Stock).SingleAsync());
        Assert.Equal(0, await database.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_MergesRepeatedProductIds()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(
            database.CustomerId, [new OrderItemReq(1, 1), new OrderItemReq(1, 2)]);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Order);
        var item = Assert.Single(result.Order.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(30m, result.Order.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_ReturnsNotFoundForUnknownProduct()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var result = await service.CreateOrderAsync(database.CustomerId, [new OrderItemReq(999, 1)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal(5, await database.Context.Products.Where(p => p.Id == 1).Select(p => p.Stock).SingleAsync());
        Assert.Equal(0, await database.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task ConfirmPaymentAsync_RejectsAnotherCustomer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var created = await service.CreateOrderAsync(database.CustomerId, [new OrderItemReq(1, 1)]);
        Assert.True(created.Success, created.Message);

        var result = await service.ConfirmPaymentAsync(created.Order!.Id, database.CustomerId + 100, "Client", "Pix");

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Equal(OrderStatuses.PendingPayment, (await database.Context.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_StoresMethodAndRejectsDuplicatePayment()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new OrderService(database.Context, NullLogger<OrderService>.Instance);
        var created = await service.CreateOrderAsync(database.CustomerId, [new OrderItemReq(1, 1)]);
        Assert.True(created.Success, created.Message);

        var paid = await service.ConfirmPaymentAsync(created.Order!.Id, database.CustomerId, "Client", "pix");

        Assert.True(paid.Success, paid.Message);
        Assert.Equal(OrderStatuses.Paid, paid.Order!.Status);
        Assert.Equal("Pix", paid.Order.PaymentMethod);
        Assert.NotNull(paid.Order.PaidAt);

        var repeated = await service.ConfirmPaymentAsync(created.Order.Id, database.CustomerId, "Client", "Card");

        Assert.False(repeated.Success);
        Assert.Equal(StatusCodes.Status409Conflict, repeated.StatusCode);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private TestDatabase(SqliteConnection connection, AppDbContext context, int customerId)
        {
            _connection = connection;
            Context = context;
            CustomerId = customerId;
        }

        public AppDbContext Context { get; }
        public int CustomerId { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var customer = new User { Username = "customer", PasswordHash = PasswordHasher.Hash("test-password"), Role = "Client" };
            context.Users.Add(customer);
            context.Products.AddRange(
                new Product { Name = "Test console", Price = 10m, Stock = 5 },
                new Product { Name = "Limited console", Price = 25m, Stock = 1 });
            await context.SaveChangesAsync();
            return new TestDatabase(connection, context, customer.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
