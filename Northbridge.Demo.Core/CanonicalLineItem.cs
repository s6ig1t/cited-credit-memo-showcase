namespace Northbridge.Demo.Core;

/// <summary>
/// A fixed, known vocabulary of financial statement line items.
///
/// WHY THIS EXISTS:
/// Real financial statements label the same concept differently across companies.
/// One borrower's statement says "Net income", another says "Net earnings", another says
/// "Profit for the year". If the Analysis engine (see Northbridge.Demo.Analysis) had to compute
/// DSCR from whatever free-text label happened to appear on a given PDF, every ratio formula
/// would need to be a fuzzy-matching exercise, which is exactly the kind of unreliable
/// behavior we do not want in the layer responsible for arithmetic.
///
/// Instead, the Extraction Agent's job includes mapping whatever label it sees in the source
/// document onto one of these fixed canonical values. The original label is preserved
/// separately (see <see cref="ExtractedLineItem.LabelAsShownInDocument"/>) for traceability,
/// but everything downstream, especially ratio calculations, keys off this enum, not off
/// free text. This is what lets the calculation engine be simple, deterministic, and testable.
/// </summary>
public enum CanonicalLineItem
{
    Unknown = 0,

    // Income statement
    TotalRevenue,
    NetIncome,
    InterestExpense,
    DepreciationAndAmortization,

    // Balance sheet
    TotalCurrentAssets,
    TotalCurrentLiabilities,
    TotalAssets,
    TotalLiabilities,
    TotalEquity,
    CashAndCashEquivalents,

    // Debt service specific
    CurrentPortionOfLongTermDebt,
    TotalDebt
}
