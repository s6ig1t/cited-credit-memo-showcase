namespace Northbridge.Demo.Core;

/// <summary>
/// Everything the Extraction Agent pulled out of one source document.
///
/// This is the boundary between "the part of the pipeline that reads a document" and
/// "the part of the pipeline that does math on numbers". Nothing downstream of this type
/// touches the source PDF again; the Analysis engine works exclusively from the
/// <see cref="LineItems"/> list here. That boundary is deliberate: it means the deterministic
/// calculation code can be unit tested with plain, hand-built instances of this record,
/// with no PDF, no HTTP call, and no AI model involved at all.
/// </summary>
/// <param name="BorrowerName">Company name as it appears on the statement.</param>
/// <param name="StatementPeriodDescription">
/// Human-readable description of the overall statement, e.g. "Fiscal Year Ended December 31, 2025".
/// </param>
/// <param name="PrimaryPeriod">
/// The period key (matching <see cref="ExtractedLineItem.Period"/>) that ratio calculations
/// should use by default, e.g. "FY2025".
///
/// WHY THIS EXISTS:
/// Financial statements routinely show two periods side by side for comparison, current
/// year and prior year. Without a declared "this one is the period we're underwriting
/// against," a lookup by canonical line item alone is ambiguous: asking for Net Income could
/// return either year's figure depending on extraction order, silently. That's the kind of
/// bug that doesn't show up against hand-built single-period test data (which is exactly
/// what this project's early unit tests used) but would surface, confusingly, the moment
/// real multi-period extraction results start flowing through. Declaring the primary period
/// explicitly, here, at the extraction-result level, removes the ambiguity at its source
/// rather than leaving each caller to guess.
/// </param>
/// <param name="LineItems">Every figure the agent was able to extract and map to a canonical concept.</param>
public record FinancialExtractionResult(
    string BorrowerName,
    string StatementPeriodDescription,
    string PrimaryPeriod,
    IReadOnlyList<ExtractedLineItem> LineItems)
{
    /// <summary>
    /// Look up a line item for the statement's <see cref="PrimaryPeriod"/>. This is what
    /// every ratio calculator in Northbridge.Demo.Analysis uses; it deliberately does NOT fall
    /// back to "just grab any matching item" if the primary period isn't found, since a
    /// silent fallback here is exactly the kind of behavior that produces a plausible-looking
    /// but wrong number.
    /// </summary>
    public ExtractedLineItem? Find(CanonicalLineItem item) => Find(item, PrimaryPeriod);

    /// <summary>
    /// Look up a line item for an explicit period. Kept as a separate overload, rather than
    /// making callers always pass a period, so calculators can stay concise for the common
    /// case while still leaving the door open for a future feature like year-over-year
    /// trend comparison, which would need to look at a non-primary period on purpose.
    /// </summary>
    public ExtractedLineItem? Find(CanonicalLineItem item, string period) =>
        LineItems.FirstOrDefault(li => li.Canonical == item && li.Period == period);
}
