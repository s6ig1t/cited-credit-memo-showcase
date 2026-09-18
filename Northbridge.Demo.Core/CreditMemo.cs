namespace Northbridge.Demo.Core;

/// <summary>
/// One section of narrative writing in the credit memo, e.g. "Cash Flow Analysis" or
/// "Leverage &amp; Debt Capacity". The Memo Agent produces these, but it is only ever given
/// already-computed <see cref="RatioResult"/> values to write about, never raw line items
/// to calculate from. This is the enforcement point of the project's central design rule:
/// by the time text generation happens, all arithmetic is already done and citation-backed.
/// </summary>
/// <param name="Heading">Section heading.</param>
/// <param name="NarrativeText">The drafted narrative for this section.</param>
/// <param name="CitedSources">
/// The union of every citation behind a number mentioned in this section's narrative, so
/// a rendered memo can offer "jump to source" links per section.
/// </param>
public record MemoSection(
    string Heading,
    string NarrativeText,
    IReadOnlyList<SourceCitation> CitedSources);

/// <summary>
/// The full, assembled credit memo: the underlying spread plus the narrative sections
/// written about it.
/// </summary>
/// <param name="BorrowerName">Carried through from the extraction for convenience at the top level.</param>
/// <param name="GeneratedAtUtc">When this memo was produced. Matters for audit trail purposes.</param>
/// <param name="Spread">The full deterministic spread this memo's narrative is grounded in.</param>
/// <param name="Sections">The drafted narrative sections.</param>
public record CreditMemo(
    string BorrowerName,
    DateTime GeneratedAtUtc,
    FinancialSpread Spread,
    IReadOnlyList<MemoSection> Sections);
