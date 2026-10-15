using System.Security.Cryptography;

namespace Ecommerce.Api.Services;

public static class PasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            input, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return Algorithm + "$" + Iterations + "$"
            + Convert.ToBase64String(salt) + "$"
            + Convert.ToBase64String(hash);
    }

    public static bool Verify(string input, string storedHash)
    {
        if (input is null || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4
            || parts[0] != Algorithm
            || !int.TryParse(parts[1], out var iterations)
            || iterations < 1 || iterations > 1_000_000)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expected.Length != HashSize)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                input, salt, iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
