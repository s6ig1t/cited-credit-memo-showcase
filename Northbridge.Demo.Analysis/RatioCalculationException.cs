namespace Northbridge.Demo.Analysis;

/// <summary>
/// Thrown by an individual ratio calculator when the extraction did not contain a line item
/// it needs. Deliberately scoped to ONE ratio failing, not the whole spread: the calling code
/// in <see cref="FinancialSpreadBuilder"/> catches this per-ratio and continues with the rest,
/// turning it into a warning message rather than letting one incomplete statement take down
/// the entire analysis. See the comment on FinancialSpread.CalculationWarnings in Core for
/// the reasoning behind that design choice.
/// </summary>
public sealed class RatioCalculationException : Exception
{
    public RatioCalculationException(string ratioName, string missingItem)
        : base($"Could not compute '{ratioName}': required line item '{missingItem}' was not found in the extraction.")
    {
    }
}
