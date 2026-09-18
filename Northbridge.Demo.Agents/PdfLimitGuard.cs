using System.Text;
using System.Text.RegularExpressions;

namespace Northbridge.Demo.Agents;

/// <summary>
/// Checks a PDF against Claude's Messages API limits before ExtractionAgent ever sends it.
/// Limits below are Anthropic's documented values as of when this was written: a 32 MB
/// maximum request payload, and 100 pages per request for requests under a 1M-token context
/// window (Claude supports up to 600 pages on the larger context window, but this project
/// targets the standard case). Anthropic can and does move these numbers over time, so if
/// this ever throws unexpectedly on a file that "should" be within limits, checking Anthropic's
/// current PDF support documentation is the first thing to do, not assuming this code is wrong.
/// </summary>
internal static class PdfLimitGuard
{
    private const long MaxRequestBytes = 32L * 1024 * 1024;
    private const int MaxPages = 100;

    /// <summary>
    /// Throws PdfTooLargeException if the PDF is definitely too large. The byte-size check
    /// is exact and always enforced, it's just pdfBytes.Length, there's no ambiguity. The
    /// page-count check is best-effort: it's a lightweight scan for page-object markers in
    /// the raw PDF bytes, not a real PDF parser, so it can undercount (or find nothing) for
    /// PDFs that store their page tree inside compressed cross-reference streams, a structure
    /// some modern PDF producers use. Rather than risk blocking a legitimate file on an
    /// unreliable count, an inconclusive page count (zero, when the file clearly has content)
    /// is treated as "unknown" and allowed through; only a CONFIDENTLY counted page total over
    /// the limit blocks the request. The byte-size check catches the cases this misses in
    /// practice, since a PDF large enough to have 100+ pages is almost always large enough to
    /// also trip the 32 MB ceiling.
    /// </summary>
    public static void EnsureWithinLimits(byte[] pdfBytes)
    {
        if (pdfBytes.LongLength > MaxRequestBytes)
        {
            throw new PdfTooLargeException(
                $"This PDF is {pdfBytes.LongLength / (1024.0 * 1024.0):0.1} MB, which exceeds Claude's " +
                $"{MaxRequestBytes / (1024 * 1024)} MB maximum request size. Split the document into smaller " +
                "sections and process each separately.");
        }

        var estimatedPages = EstimatePageCount(pdfBytes);

        if (estimatedPages > MaxPages)
        {
            throw new PdfTooLargeException(
                $"This PDF appears to have approximately {estimatedPages} pages, which exceeds Claude's " +
                $"{MaxPages}-page limit per request. Split the document into sections of {MaxPages} pages " +
                "or fewer and process each separately.");
        }
    }

    /// <summary>
    /// Best-effort page count: counts "/Type /Page" object markers in the raw PDF bytes,
    /// carefully excluding "/Type /Pages" (the page TREE node, not an individual page).
    /// This works for PDFs with an uncompressed page tree, which includes the sample PDF
    /// this project generates. It will likely return 0 (or an undercount) on PDFs using
    /// compressed object streams, since those bytes are deflate-compressed and invisible to
    /// a raw scan. A real PDF parsing library would count reliably in every case; this
    /// avoids taking on that dependency just to produce one number for a pre-flight check.
    /// </summary>
    private static int EstimatePageCount(byte[] pdfBytes)
    {
        // Latin1 (ISO-8859-1) decodes every byte value 0-255 to exactly one character with
        // no loss or exceptions, which is what we want here: we're pattern-matching raw
        // bytes as if they were text, not correctly decoding the PDF's actual text content.
        var raw = Encoding.Latin1.GetString(pdfBytes);

        // Matches "/Type /Page" or "/Type/Page" but not "/Type /Pages" (negative lookahead
        // on a trailing 's'), and not "/Type /Pagemumble" or similar (word boundary).
        var matches = Regex.Matches(raw, @"/Type\s*/Page(?!s)\b");

        return matches.Count;
    }
}
