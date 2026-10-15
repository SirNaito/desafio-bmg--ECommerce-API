using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Username).IsUnique();
        modelBuilder.Entity<Product>().Property(product => product.Name).HasMaxLength(120).IsRequired();
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(order => order.Total).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(item => item.UnitPrice).HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasOne<User>().WithMany().HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasMany(order => order.Items).WithOne()
            .HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderItem>()
            .HasOne(item => item.Product).WithMany()
            .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
