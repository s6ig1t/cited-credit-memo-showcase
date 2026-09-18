namespace Northbridge.Demo.Core;

/// <summary>
/// A "spread" in commercial lending is the standard term for a borrower's financials laid
/// out in a normalized, comparable format, plus the ratios computed from them. This type is
/// the C# representation of that artifact: the extracted source data plus every ratio
/// computed from it, deterministically, before any narrative writing happens.
/// </summary>
/// <param name="Extraction">The raw extracted, canonically-mapped line items.</param>
/// <param name="Ratios">Every ratio computed from those line items, each with its own citations and policy flag.</param>
/// <param name="CalculationWarnings">
/// Human-readable notes for any ratio that could NOT be computed, typically because the
/// source document was missing a required line item. Real financial statements are messy
/// and incomplete more often than not, so the calculation engine is built to skip what it
/// can't compute rather than fail the entire spread over one missing figure. A reviewer
/// (or the Memo Agent) can see exactly what was skipped and why, rather than the pipeline
/// silently pretending a ratio was never relevant.
/// </param>
public record FinancialSpread(
    FinancialExtractionResult Extraction,
    IReadOnlyList<RatioResult> Ratios,
    IReadOnlyList<string> CalculationWarnings);
