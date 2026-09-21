using System.Security.Cryptography;
using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The one implementation of how a password or a temporary PIN is turned into the hash and salt the store holds,
/// and how a presented secret is checked against them.
/// </summary>
/// <remarks>
/// <para>
/// A reset in the user-management path writes a credential that the sign-in path has to accept, so the two must
/// use the same parameters. They used to be two copies of the same arithmetic in two projects; a change to one
/// would have made every PIN issued from the other silently unusable, and nothing in the build or the suite could
/// have seen it. This type is that single place: PBKDF2 with SHA-256, 100,000 iterations, a 32-byte hash and a
/// 16-byte salt.
/// </para>
/// <para>
/// Only the salted hash is ever stored. The secret itself is never stored, never logged and never written to
/// history (FR-030).
/// </para>
/// </remarks>
public static class PasswordSecretHasher
{
    /// <summary>The iteration count both paths use.</summary>
    public const int Iterations = 100_000;

    /// <summary>The salt length in bytes.</summary>
    public const int SaltLengthBytes = 16;

    /// <summary>The hash length in bytes.</summary>
    public const int HashLengthBytes = 32;

    /// <summary>
    /// A fresh random salt for a new or replaced credential.
    /// </summary>
    public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(SaltLengthBytes);

    /// <summary>
    /// The base64 hash of <paramref name="secret"/> under <paramref name="salt"/>, as the store holds it.
    /// </summary>
    public static string Hash(string secret, byte[] salt)
    {
        ArgumentNullException.ThrowIfNull(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secret ?? string.Empty),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLengthBytes);

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Whether <paramref name="secret"/> is the credential <paramref name="storedHash"/> and
    /// <paramref name="storedSalt"/> were made from. Compared in fixed time.
    /// </summary>
    public static bool Verify(string? secret, string? storedHash, byte[]? storedSalt)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || storedSalt is null || storedSalt.Length == 0)
        {
            return false;
        }

        byte[] expectedHash;
        try
        {
            expectedHash = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secret ?? string.Empty),
            storedSalt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLengthBytes);

        return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
    }
}
