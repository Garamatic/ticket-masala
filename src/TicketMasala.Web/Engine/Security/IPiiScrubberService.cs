namespace TicketMasala.Web.Engine.Security;

/// <summary>
/// Service responsible for detecting and scrubbing Personally Identifiable Information (PII)
/// from text content before it is stored or processed.
/// </summary>
public interface IPiiScrubberService
{
    /// <summary>
    /// Scrub sensitive information from the input text.
    /// </summary>
    /// <param name="input">The raw text containing potential PII.</param>
    /// <returns>The sanitized text with PII replaced by [REDACTED] placeholders.</returns>
    string Scrub(string input);
}
