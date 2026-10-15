using System.Text;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=ecommerce.db"));
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<JwtService>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret must be configured and contain at least 32 bytes.");
}

var issuer = builder.Configuration["Jwt:Issuer"] ?? "BigEcom";
var audience = builder.Configuration["Jwt:Audience"] ?? "BigEcomAudience";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User { Username = "Admin", PasswordHash = PasswordHasher.Hash("Pass123@"), Role = "Admin" },
                new User { Username = "Gabriel", PasswordHash = PasswordHasher.Hash("Pass123@"), Role = "Client" },
                new User { Username = "Sayo", PasswordHash = PasswordHasher.Hash("Pass@123"), Role = "Client" });
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Steam Deck OLED 512GB", Price = 4999.90m, Stock = 200 },
                new Product { Name = "ROG Ally 1TB", Price = 7999.90m, Stock = 100 },
                new Product { Name = "Nintendo Switch OLED", Price = 3499.90m, Stock = 300 },
                new Product { Name = "Xbox Series S", Price = 2499.90m, Stock = 150 },
                new Product { Name = "PlayStation 5", Price = 4499.90m, Stock = 180 });
        }

        await db.SaveChangesAsync();
    }
}

app.Run();

public partial class Program { }
