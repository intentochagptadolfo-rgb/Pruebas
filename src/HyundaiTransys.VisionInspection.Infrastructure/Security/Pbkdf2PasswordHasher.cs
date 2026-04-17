using System.Security.Cryptography;
using HyundaiTransys.VisionInspection.Core.Abstractions;

namespace HyundaiTransys.VisionInspection.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var expected = Convert.FromBase64String(hash);
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, Algorithm, HashBytes);
        return CryptographicOperations.FixedTimeEquals(expected, computed);
    }
}
