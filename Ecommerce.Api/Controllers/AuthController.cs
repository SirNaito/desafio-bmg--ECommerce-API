using Microsoft.AspNetCore.Mvc;
using Ecommerce.Api.DTOs;
using Ecommerce.Api.Data;
using Ecommerce.Api.Models;
using Ecommerce.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                return Unauthorized("Usuário ou senha inválidos");

            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }
    }
}
