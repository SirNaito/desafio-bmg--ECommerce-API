using Ecommerce.Api.Data;
using Ecommerce.Api.Services;
using Microsoft.EntityFrameworkCore;
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
            new Models.User { Username = "Admin", PasswordHash = Services.PasswordHasher.Hash("Pass123@"), Role = "Admin" },
            new Models.User { Username = "Gabriel", PasswordHash = Services.PasswordHasher.Hash("Pass123@"), Role = "Client" },
            new Models.User { Username = "Sayo", PasswordHash = Services.PasswordHasher.Hash("Pass@123"), Role = "Client" }
        );
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Models.Product { Name = "Steam Deck Oled 512GB", Price = 4999.90m, Stock = 200},
            new Models.Product { Name = "Rog Ally Pro 1TB", Price = 7999.90m, Stock = 100},
            new Models.Product { Name = "Nintendo Switch Oled", Price = 3499.90m, Stock = 300},
            new Models.Product { Name = "Xbox Series S", Price = 2499.90m, Stock = 150},
            new Models.Product { Name = "Playstation 5", Price = 4499.90m, Stock = 180}
        );
    }

    db.SaveChanges();
}

app.Urls.Add("http://localhost:5162/Swagger");
app.Run();
