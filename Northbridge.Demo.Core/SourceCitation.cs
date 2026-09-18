namespace Northbridge.Demo.Core;

/// <summary>
/// A pointer back to the exact place in the source document a piece of data came from.
///
/// WHY THIS EXISTS:
/// Northbridge's own stated differentiator is that every number in a credit memo traces back
/// to its source document and page. This type is how we make that traceability a first-class
/// citizen of the data model instead of an afterthought bolted onto a UI layer. Every
/// <see cref="ExtractedLineItem"/> and every claim in a <see cref="MemoSection"/> carries
/// one or more of these, so "where did this number come from" is always answerable by
/// following data, not by re-reading the source PDF by hand.
/// </summary>
/// <param name="PageNumber">
/// 1-based page number within the source PDF. We ask Claude to report this directly when
/// it reads the document natively, since it can see page boundaries the same way a human
/// reader would.
/// </param>
/// <param name="Snippet">
/// A short verbatim-ish excerpt (a few words) from the source page, near where the value
/// was found. This is a human sanity-check: even if the page number were ever wrong, a
/// reviewer scanning that page for this snippet can confirm or correct the citation by eye.
/// Kept short deliberately; this is a locator, not a reproduction of the source document.
/// </param>
public record SourceCitation(int PageNumber, string? Snippet = null);
