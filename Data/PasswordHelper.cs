using System.Security.Cryptography;

namespace FoodOrderingSystem.Data
{
    // Manual password hashing since ASP.NET Core Identity is not allowed by the assignment.
    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            var bytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(bytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using var derive = new Rfc2898DeriveBytes(
                password,
                Convert.FromBase64String(salt),
                100_000,
                HashAlgorithmName.SHA256);

            return Convert.ToBase64String(derive.GetBytes(32));
        }

        public static bool VerifyPassword(string password, string salt, string storedHash)
        {
            var computedHash = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedHash),
                Convert.FromBase64String(storedHash));
        }
    }
}
