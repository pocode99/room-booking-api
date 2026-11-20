using System.Security.Cryptography;
using RoomBooking.Application.Auth;

namespace RoomBooking.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher
{
    private const string Version = "v1";
    private const int SaltSize = 16;          // bytes
    private const int KeySize = 32;           // bytes
    private const int Iterations = 600_000;   // PBKDF2-HMAC-SHA256

    public string Hash(string password)
    {
        // A fresh random salt per password, so equal passwords get different hashes.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // Format: version.iterations.salt.hash (Base64 never contains a dot).
        return $"{Version}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 4 || parts[0] != Version) return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations < 1) return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Constant-time comparison to avoid leaking information through timing.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}