using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// Small internal helper shared by every ratio calculator: look up a required canonical
/// line item, or throw a <see cref="RatioCalculationException"/> naming exactly which ratio
/// and which missing figure caused the failure.
///
/// Centralizing this one lookup-or-throw pattern keeps each individual calculator (see
/// <see cref="DscrCalculator"/>, <see cref="LiquidityCalculator"/>, <see cref="LeverageCalculator"/>)
/// reading as close to the actual financial formula as possible, without repeating null-check
/// boilerplate in every one of them.
/// </summary>
internal static class RequiredLineItem
{
    public static ExtractedLineItem Get(FinancialExtractionResult extraction, CanonicalLineItem item, string ratioName)
    {
        var found = extraction.Find(item);
        if (found is null)
        {
            throw new RatioCalculationException(ratioName, item.ToString());
        }

        return found;
    }
}
