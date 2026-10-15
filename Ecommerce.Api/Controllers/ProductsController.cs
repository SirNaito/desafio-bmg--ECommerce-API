using Ecommerce.Api.Data;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("products")]
[Authorize(Roles = "Admin")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> GetAll()
    {
        var products = await db.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync();
        return Ok(products.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await db.Products.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductDto dto)
    {
        var product = new Product { Name = dto.Name.Trim(), Price = dto.Price, Stock = dto.Stock };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductDto dto)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Name = dto.Name.Trim();
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        await db.SaveChangesAsync();
        return Ok(ToResponse(product));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        if (await db.OrderItems.AnyAsync(item => item.ProductId == id))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Product cannot be deleted",
                Detail = "This product is referenced by an order."
            });
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Price, product.Stock);
}
