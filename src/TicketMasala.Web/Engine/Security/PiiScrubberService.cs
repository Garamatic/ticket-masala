using System.Text.RegularExpressions;

namespace TicketMasala.Web.Engine.Security;

/// <summary>
/// Implementation of PII Scrubber using Regex pattern matching.
/// Handles Emails, Phone Numbers, and Belgian National Registry Numbers (NISS).
/// </summary>
public class PiiScrubberService : IPiiScrubberService
{
    private readonly ILogger<PiiScrubberService> _logger;

    // Regex Patterns
    // Email: Standard email pattern
    private static readonly Regex EmailRegex = new Regex(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Phone: Matches international formats (+32...) and local formats (04...)
    // Be careful not to match random numbers. We look for at least 9 digits with optional separators.
    private static readonly Regex PhoneRegex = new Regex(
        @"(?:\+|00)[1-9]\d{0,3}[\s.-]?\(?0?\)?[\s.-]?\d{2,4}[\s.-]?\d{2,4}[\s.-]?\d{2,4}|\b04\d{2}[\s.-]?\d{2}[\s.-]?\d{2}[\s.-]?\d{2}\b", 
        RegexOptions.Compiled);

    // NISS (Rijksregisternummer): XX.XX.XX-XXX.XX
    private static readonly Regex NissRegex = new Regex(
        @"\b\d{2}\.\d{2}\.\d{2}-\d{3}\.\d{2}\b", 
        RegexOptions.Compiled);

    public PiiScrubberService(ILogger<PiiScrubberService> logger)
    {
        _logger = logger;
    }

    public string Scrub(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var originalLength = input.Length;
        var scrubbed = input;

        // 1. Scrub Emails
        scrubbed = EmailRegex.Replace(scrubbed, "[EMAIL_REDACTED]");

        // 2. Scrub NISS (High Priority, very specific format)
        scrubbed = NissRegex.Replace(scrubbed, "[NISS_REDACTED]");

        // 3. Scrub Phones
        scrubbed = PhoneRegex.Replace(scrubbed, "[PHONE_REDACTED]");

        if (scrubbed.Length != originalLength)
        {
            // _logger.LogDebug("Scrubbed PII from content. Original length: {Original}, New length: {New}", originalLength, scrubbed.Length);
        }

        return scrubbed;
    }
}
