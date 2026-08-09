using System.Security.Cryptography;
using System.Text;

namespace DepartmentFinancialRecords.API.Utilities
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(salt.Concat(passwordBytes).ToArray());
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            var saltBytes = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            using var sha = SHA256.Create();
            var computedHash = sha.ComputeHash(saltBytes.Concat(passwordBytes).ToArray());

            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
    }
}
