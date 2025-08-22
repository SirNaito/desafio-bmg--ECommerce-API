using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Api.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static bool Verify(string input, string hash) => Hash(input) == hash;
    }
}
