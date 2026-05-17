using System.Security.Cryptography;
using System.Text;

namespace Testers.Infrastructure.Auth;

// SHA-256 (no salt) is enough: keys are 256 bits of entropy, brute-force isn't the threat.
// Deterministic hash means auth can look up by exact match.
public static class ApiKeyHasher
{
    // Returns the raw key (show to admin once), the hash (store in DB), and a short prefix (for display).
    public static (string Raw, string Hash, string Prefix) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (raw, Hash(raw), raw[..8]);
    }

    public static string Hash(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);  // 64 chars uppercase
    }
}
