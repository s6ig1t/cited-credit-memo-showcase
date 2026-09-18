namespace Northbridge.Demo.Core;

/// <summary>
/// One financial ratio, computed by plain C# arithmetic (see Northbridge.Demo.Analysis), never
/// by an AI model. This type is what an LLM is handed when it drafts memo narrative; it is
/// never asked to produce these values itself.
/// </summary>
/// <param name="Name">e.g. "Debt Service Coverage Ratio (DSCR)".</param>
/// <param name="Value">The computed value.</param>
/// <param name="FormulaDescription">
/// Plain-English formula used, e.g. "(Net Income + Depreciation & Amortization + Interest Expense) /
/// (Interest Expense + Current Portion of Long-Term Debt)". Stored alongside the number so a
/// reviewer, or the Memo Agent, never has to guess how a value was derived.
/// </param>
/// <param name="SupportingCitations">
/// Every source citation for the line items that fed into this calculation, flattened into
/// one list. This is what lets a rendered memo make a ratio's number clickable straight back
/// to every source page that contributed to it.
/// </param>
/// <param name="Flag">The policy-check result for this ratio, if a policy rule applies to it.</param>
public record RatioResult(
    string Name,
    decimal Value,
    string FormulaDescription,
    IReadOnlyList<SourceCitation> SupportingCitations,
    PolicyFlag Flag);
