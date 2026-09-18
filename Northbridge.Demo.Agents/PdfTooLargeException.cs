namespace Northbridge.Demo.Agents;

/// <summary>
/// Thrown when a source PDF exceeds Claude's Messages API limits, caught before any HTTP
/// request is made. Failing here, with a specific, actionable message, is deliberately
/// preferred over letting Anthropic's API reject an oversized request: a 400 from the API
/// would still surface eventually, but as a generic HTTP error deep in ExtractAsync, with
/// none of the specifics (how large, how many pages, what the actual limit is) that make
/// this exception's message useful to whoever reads it.
/// </summary>
public sealed class PdfTooLargeException : Exception
{
    public PdfTooLargeException(string message) : base(message)
    {
    }
}
