using System.Security.Cryptography;

namespace PROG2026_part2.Services;

/// <summary>
/// Hashes and verifies passwords so the original password is never stored.
/// Uses PBKDF2 (SHA-256, 100 000 iterations) with a random 16-byte salt per user.
/// </summary>
public class PasswordService
{
    /// <summary>
    /// Creates a new random salt and returns it together with the password hash (both Base64).
    /// </summary>
    public PasswordResult CreatePasswordHash(string password)
    {
        // A unique salt per password means identical passwords produce different hashes.
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return new PasswordResult
        {
            Hash = Convert.ToBase64String(hash),
            Salt = Convert.ToBase64String(salt)
        };
    }

    /// <summary>
    /// Re-hashes the supplied password with the stored salt and compares it with the stored hash.
    /// </summary>
    public bool VerifyPassword(
        string password,
        string storedHash,
        string storedSalt)
    {
        byte[] salt = Convert.FromBase64String(storedSalt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        string calculatedHash = Convert.ToBase64String(hash);

        return calculatedHash == storedHash;
    }
}

/// <summary>
/// The salt and hash produced when a password is hashed.
/// </summary>
public class PasswordResult
{
    public string Hash { get; set; } = string.Empty;

    public string Salt { get; set; } = string.Empty;
}
