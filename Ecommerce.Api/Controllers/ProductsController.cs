using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Api.DTOs;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync());

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Product name cannot be blank.");

            var product = new Product { Name = dto.Name.Trim(), Price = dto.Price, Stock = dto.Stock };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Product name cannot be blank.");

            var product = await _db.Products.FindAsync(id);
            if (product is null) return NotFound();
            product.Name = dto.Name.Trim();
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            await _db.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null) return NotFound();
            if (await _db.OrderItems.AnyAsync(item => item.ProductId == id))
                return Conflict("This product is referenced by an order and cannot be deleted.");

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
