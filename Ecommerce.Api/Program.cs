using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("EcommerceDb"));

builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<JwtService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "dev-secret";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BigEcom";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BigEcomAudience";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//Banco de dados em memória
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { Username = "Admin", PasswordHash = PasswordHasher.Hash("Pass123@"), Role = "Admin" },
            new User { Username = "Gabriel", PasswordHash = PasswordHasher.Hash("Pass123@"), Role = "Client" },
            new User { Username = "Sayo", PasswordHash = PasswordHasher.Hash("Pass@123"), Role = "Client" }
        );
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Steam Deck Oled 512GB", Price = 4999.90m, Stock = 200},
            new Product { Name = "Rog Ally Pro 1TB", Price = 7999.90m, Stock = 100},
            new Product { Name = "Nintendo Switch Oled", Price = 3499.90m, Stock = 300},
            new Product { Name = "Xbox Series S", Price = 2499.90m, Stock = 150},
            new Product { Name = "Playstation 5", Price = 4499.90m, Stock = 180}
        );
    }

    db.SaveChanges();
}

app.Run();

// Para executar o Swagger em uma URL específica, use o seguinte comando:
// dotnet run --urls "http://localhost:5162/Swagger"
