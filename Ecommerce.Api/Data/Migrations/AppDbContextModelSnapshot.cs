using Ecommerce.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Ecommerce.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");

        modelBuilder.Entity("Ecommerce.Api.Models.Order", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            entity.Property<DateTime?>("PaidAt").HasColumnType("TEXT");
            entity.Property<string>("PaymentMethod").HasColumnType("TEXT");
            entity.Property<string>("Status").IsRequired().HasColumnType("TEXT");
            entity.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("TEXT");
            entity.Property<int>("UserId").HasColumnType("INTEGER");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("Orders");
        });

        modelBuilder.Entity("Ecommerce.Api.Models.OrderItem", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<int>("OrderId").HasColumnType("INTEGER");
            entity.Property<int>("ProductId").HasColumnType("INTEGER");
            entity.Property<int>("Quantity").HasColumnType("INTEGER");
            entity.Property<decimal>("UnitPrice").HasPrecision(18, 2).HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("OrderId");
            entity.HasIndex("ProductId");
            entity.ToTable("OrderItems");
        });

        modelBuilder.Entity("Ecommerce.Api.Models.Product", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            entity.Property<decimal>("Price").HasPrecision(18, 2).HasColumnType("TEXT");
            entity.Property<int>("Stock").HasColumnType("INTEGER");
            entity.HasKey("Id");
            entity.ToTable("Products");
        });

        modelBuilder.Entity("Ecommerce.Api.Models.User", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);
            entity.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Role").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Username").IsRequired().HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("Username").IsUnique();
            entity.ToTable("Users");
        });

        modelBuilder.Entity("Ecommerce.Api.Models.Order", entity =>
        {
            entity.HasOne("Ecommerce.Api.Models.User", null).WithMany()
                .HasForeignKey("UserId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });

        modelBuilder.Entity("Ecommerce.Api.Models.OrderItem", entity =>
        {
            entity.HasOne("Ecommerce.Api.Models.Order", null).WithMany("Items")
                .HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.HasOne("Ecommerce.Api.Models.Product", "Product").WithMany()
                .HasForeignKey("ProductId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.Navigation("Product");
        });

        modelBuilder.Entity("Ecommerce.Api.Models.Order", entity => entity.Navigation("Items"));
    }
}
