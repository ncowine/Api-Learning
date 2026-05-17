using System.Security.Cryptography;
using System.Text;

namespace Testers.Infrastructure.Auth;

/// <summary>
/// Generates and hashes API keys.
///
/// API keys are high-entropy random tokens (32 bytes / 256 bits of randomness), so SHA-256 is
/// sufficient — we don't need BCrypt's deliberate slowness here (that's for low-entropy passwords
/// where brute-force is a real threat). SHA-256 is fast, which matters since we hash on every
/// authenticated request.
///
/// Format: 256 random bits encoded as URL-safe Base64 (43 chars, no padding) → e.g.
/// <c>"x7Hg-pQs9mNc4LRy8tF2v5BkW3oZ_aE6jU0i7AeD9hH"</c>. The first 8 chars are stored as the
/// <c>Prefix</c> for display.
/// </summary>
public static class ApiKeyHasher
{
    /// <summary>
    /// Generates a new key. Returns the raw key (give to caller ONCE — never reconstructible),
    /// its hash (store in DB), and a short prefix (store for display).
    /// </summary>
    public static (string Raw, string Hash, string Prefix) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var hash = Hash(raw);
        var prefix = raw[..8];
        return (raw, hash, prefix);
    }

    /// <summary>
    /// Hashes a raw key for storage / lookup. Deterministic (no salt) so the auth handler can
    /// hash the incoming header and look the row up by exact match.
    /// </summary>
    public static string Hash(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes); // 64 uppercase hex chars
    }
}
