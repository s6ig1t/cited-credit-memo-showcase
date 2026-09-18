using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// The public entry point for the entire Analysis project. Everything outside this project
/// (the Api layer, eventually the Memo Agent) should only ever need to call
/// <see cref="Build"/>. Everything else in this project (the individual calculators,
/// PolicyRules, RequiredLineItem) is implementation detail behind that one method.
/// </summary>
public static class FinancialSpreadBuilder
{
    /// <summary>
    /// Runs every known ratio calculation against one extraction result. Each calculator
    /// runs independently: if one fails because the source document was missing a required
    /// figure, that failure is caught, turned into a plain-English warning, and the rest of
    /// the calculators still run. A single missing line item degrades the spread; it does
    /// not crash it.
    /// </summary>
    public static FinancialSpread Build(FinancialExtractionResult extraction)
    {
        var ratios = new List<RatioResult>();
        var warnings = new List<string>();

        // Each entry is a named calculation step. Adding a new ratio in the future means
        // adding one line here, not touching the orchestration logic below.
        var calculators = new (string Name, Func<FinancialExtractionResult, RatioResult> Calculate)[]
        {
            (DscrCalculator.RatioName, DscrCalculator.Calculate),
            (LiquidityCalculator.CurrentRatioName, LiquidityCalculator.CalculateCurrentRatio),
            (LiquidityCalculator.WorkingCapitalName, LiquidityCalculator.CalculateWorkingCapital),
            (LeverageCalculator.RatioName, LeverageCalculator.CalculateDebtToEquity),
        };

        foreach (var (name, calculate) in calculators)
        {
            try
            {
                ratios.Add(calculate(extraction));
            }
            catch (RatioCalculationException ex)
            {
                // Deliberately caught here and only here: this is the one place in the
                // pipeline where "a ratio could not be computed" is an expected, handled
                // outcome rather than a bug. Anywhere else, a RatioCalculationException
                // escaping would indicate something genuinely wrong.
                warnings.Add(ex.Message);
            }
        }

        return new FinancialSpread(extraction, ratios, warnings);
    }
}
