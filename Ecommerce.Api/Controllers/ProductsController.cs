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
    [Authorize(Roles="Admin")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll() => Ok(await _db.Products.ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            var product = new Product { Name = dto.Name, Price = dto.Price, Stock = dto.Stock };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            await _db.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
