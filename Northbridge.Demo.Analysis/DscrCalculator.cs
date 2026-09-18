using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// Computes the Debt Service Coverage Ratio: the single most-watched metric in commercial
/// lending, since it answers the question a credit committee cares about most, can this
/// borrower's operating cash flow actually cover its debt payments.
///
/// This is plain arithmetic over already-extracted numbers. Nothing here calls an AI model,
/// and nothing here can be influenced by how an LLM "feels" about a borrower; it is exactly
/// as reliable as a spreadsheet formula, because that is essentially what it is.
/// </summary>
public static class DscrCalculator
{
    public const string RatioName = "Debt Service Coverage Ratio (DSCR)";

    public static RatioResult Calculate(FinancialExtractionResult extraction)
    {
        var netIncome = RequiredLineItem.Get(extraction, CanonicalLineItem.NetIncome, RatioName);
        var depreciationAndAmortization = RequiredLineItem.Get(extraction, CanonicalLineItem.DepreciationAndAmortization, RatioName);
        var interestExpense = RequiredLineItem.Get(extraction, CanonicalLineItem.InterestExpense, RatioName);
        var currentPortionOfLongTermDebt = RequiredLineItem.Get(extraction, CanonicalLineItem.CurrentPortionOfLongTermDebt, RatioName);

        // Cash available for debt service adds back non-cash D&A, and adds back interest expense
        // before dividing by total debt service, since interest is part of what's being covered.
        var cashAvailableForDebtService = netIncome.Value + depreciationAndAmortization.Value + interestExpense.Value;
        var totalDebtService = interestExpense.Value + currentPortionOfLongTermDebt.Value;

        if (totalDebtService == 0)
        {
            // A zero denominator here almost always signals bad or incomplete source data
            // (a borrower with genuinely zero debt service wouldn't need a DSCR check at all).
            // We fail loudly rather than silently returning an infinite or meaningless ratio.
            throw new RatioCalculationException(RatioName, "non-zero total debt service (interest expense + current portion of long-term debt)");
        }

        var dscr = Math.Round(cashAvailableForDebtService / totalDebtService, 2);

        var citations = new[]
            {
                netIncome.Citation,
                depreciationAndAmortization.Citation,
                interestExpense.Citation,
                currentPortionOfLongTermDebt.Citation
            }
            .Distinct()
            .ToList();

        return new RatioResult(
            RatioName,
            dscr,
            "(Net Income + Depreciation & Amortization + Interest Expense) / (Interest Expense + Current Portion of Long-Term Debt)",
            citations,
            PolicyRules.EvaluateDscr(dscr));
    }
}
