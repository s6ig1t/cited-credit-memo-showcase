using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// Computes short-term liquidity metrics: Current Ratio and Working Capital. Both use the
/// same two inputs (current assets, current liabilities), so they're grouped in one
/// calculator rather than duplicating the same two lookups across two separate files.
/// </summary>
public static class LiquidityCalculator
{
    public const string CurrentRatioName = "Current Ratio";
    public const string WorkingCapitalName = "Working Capital";

    public static RatioResult CalculateCurrentRatio(FinancialExtractionResult extraction)
    {
        var currentAssets = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalCurrentAssets, CurrentRatioName);
        var currentLiabilities = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalCurrentLiabilities, CurrentRatioName);

        if (currentLiabilities.Value == 0)
        {
            throw new RatioCalculationException(CurrentRatioName, "non-zero total current liabilities");
        }

        var ratio = Math.Round(currentAssets.Value / currentLiabilities.Value, 2);

        var citations = new[] { currentAssets.Citation, currentLiabilities.Citation }.Distinct().ToList();

        return new RatioResult(
            CurrentRatioName,
            ratio,
            "Total Current Assets / Total Current Liabilities",
            citations,
            PolicyRules.EvaluateCurrentRatio(ratio));
    }

    public static RatioResult CalculateWorkingCapital(FinancialExtractionResult extraction)
    {
        var currentAssets = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalCurrentAssets, WorkingCapitalName);
        var currentLiabilities = RequiredLineItem.Get(extraction, CanonicalLineItem.TotalCurrentLiabilities, WorkingCapitalName);

        var workingCapital = currentAssets.Value - currentLiabilities.Value;

        var citations = new[] { currentAssets.Citation, currentLiabilities.Citation }.Distinct().ToList();

        return new RatioResult(
            WorkingCapitalName,
            workingCapital,
            "Total Current Assets - Total Current Liabilities",
            citations,
            PolicyRules.EvaluateWorkingCapital(workingCapital));
    }
}
