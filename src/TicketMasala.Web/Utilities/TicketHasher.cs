using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TicketMasala.Web.Utilities;

public static class TicketHasher
{
    private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Computes a canonicalized SHA256 hash of the ticket content (Description + CustomerId).
    /// Used for robust duplicate detection.
    /// Canonicalization:
    /// 1. Lowercase
    /// 2. Trim whitespace
    /// 3. Collapse multiple spaces to single space
    /// </summary>
    public static string ComputeContentHash(string description, string customerId)
    {
        // 1. Sanitize
        var raw = $"{customerId}|{description}".ToLowerInvariant();

        // 2. Normalize whitespace
        var clean = WhitespaceRegex.Replace(raw, " ").Trim();

        // 3. Hash
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clean));
        return Convert.ToHexString(bytes);
    }
}
