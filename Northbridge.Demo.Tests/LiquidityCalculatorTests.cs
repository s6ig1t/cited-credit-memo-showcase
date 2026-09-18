using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Xunit;
using static Northbridge.Demo.Tests.TestData;

namespace Northbridge.Demo.Tests;

public class LiquidityCalculatorTests
{
    [Fact]
    public void CalculateCurrentRatio_HealthyBalance_ReturnsExpectedValueAndNoFlag()
    {
        // 500,000 / 300,000 = 1.6667, rounds to 1.67
        var extraction = Build("Example Fabrication LLC",
            Item(CanonicalLineItem.TotalCurrentAssets, 500_000m),
            Item(CanonicalLineItem.TotalCurrentLiabilities, 300_000m));

        var result = LiquidityCalculator.CalculateCurrentRatio(extraction);

        Assert.Equal(1.67m, result.Value);
        Assert.Equal(PolicyFlagSeverity.None, result.Flag.Severity);
    }

    [Fact]
    public void CalculateCurrentRatio_LiabilitiesExceedAssets_ReturnsBreachSeverity()
    {
        var extraction = Build("Underwater LLC",
            Item(CanonicalLineItem.TotalCurrentAssets, 250_000m),
            Item(CanonicalLineItem.TotalCurrentLiabilities, 300_000m));

        var result = LiquidityCalculator.CalculateCurrentRatio(extraction);

        Assert.Equal(0.83m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Breach, result.Flag.Severity);
    }

    [Fact]
    public void CalculateCurrentRatio_MissingCurrentLiabilities_Throws()
    {
        var extraction = Build("Incomplete Statement LLC",
            Item(CanonicalLineItem.TotalCurrentAssets, 500_000m));

        Assert.Throws<RatioCalculationException>(() => LiquidityCalculator.CalculateCurrentRatio(extraction));
    }

    [Fact]
    public void CalculateWorkingCapital_PositiveBalance_ReturnsRawDollarAmountAndNoFlag()
    {
        var extraction = Build("Example Fabrication LLC",
            Item(CanonicalLineItem.TotalCurrentAssets, 500_000m),
            Item(CanonicalLineItem.TotalCurrentLiabilities, 300_000m));

        var result = LiquidityCalculator.CalculateWorkingCapital(extraction);

        // Working capital is a dollar amount, not a ratio, so no rounding to 2 decimal
        // places the way ratio values are; it should be the exact difference.
        Assert.Equal(200_000m, result.Value);
        Assert.Equal(PolicyFlagSeverity.None, result.Flag.Severity);
    }

    [Fact]
    public void CalculateWorkingCapital_NegativeBalance_ReturnsBreachSeverity()
    {
        var extraction = Build("Underwater LLC",
            Item(CanonicalLineItem.TotalCurrentAssets, 250_000m),
            Item(CanonicalLineItem.TotalCurrentLiabilities, 300_000m));

        var result = LiquidityCalculator.CalculateWorkingCapital(extraction);

        Assert.Equal(-50_000m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Breach, result.Flag.Severity);
    }
}
