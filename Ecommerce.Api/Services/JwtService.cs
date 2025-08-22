using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ecommerce.Api.Models;

namespace Ecommerce.Api.Services
{
    public class JwtService
    {
        private readonly IConfiguration _cfg;
        public JwtService(IConfiguration cfg) { _cfg = cfg; }

        public string GenerateToken(User user)
        {
            var secret = _cfg["Jwt:Secret"] ?? "dev-secret";
            var issuer = _cfg["Jwt:Issuer"] ?? "BigEcom";
            var audience = _cfg["Jwt:Audience"] ?? "BigEcomAudience";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
