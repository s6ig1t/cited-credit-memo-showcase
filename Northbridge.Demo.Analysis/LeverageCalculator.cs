using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// Computes Debt-to-Equity: how leveraged the borrower is relative to its own equity cushion.
/// Uses Total Liabilities rather than only interest-bearing debt, which is the more
/// conservative and more commonly required version of this ratio in commercial underwriting.
/// </summary>
public static class LeverageCalculator
{
    public const string RatioName = "Debt-to-Equity Ratio";

    public static RatioResult CalculateDebtToEquity(FinancialExtractionResult extraction)
    {
        var totalLiabilities = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalLiabilities, RatioName);
        var totalEquity = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalEquity, RatioName);

        if (totalEquity.Value == 0)
        {
            throw new RatioCalculationException(RatioName, "non-zero total equity");
        }

        var ratio = Math.Round(totalLiabilities.Value / totalEquity.Value, 2);

        var citations = new[] { totalLiabilities.Citation, totalEquity.Citation }.Distinct().ToList();

        return new RatioResult(
            RatioName,
            ratio,
            "Total Liabilities / Total Equity",
            citations,
            PolicyRules.EvaluateDebtToEquity(ratio));
    }
}
