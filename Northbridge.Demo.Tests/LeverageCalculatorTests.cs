using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Xunit;
using static Northbridge.Demo.Tests.TestData;

namespace Northbridge.Demo.Tests;

public class LeverageCalculatorTests
{
    [Fact]
    public void CalculateDebtToEquity_ConservativeLeverage_ReturnsNoFlag()
    {
        // 900,000 / 600,000 = 1.50, below the 2.00 watch threshold
        var extraction = Build("Example Fabrication LLC",
            Item(CanonicalLineItem.TotalLiabilities, 900_000m),
            Item(CanonicalLineItem.TotalEquity, 600_000m));

        var result = LeverageCalculator.CalculateDebtToEquity(extraction);

        Assert.Equal(1.50m, result.Value);
        Assert.Equal(PolicyFlagSeverity.None, result.Flag.Severity);
    }

    [Fact]
    public void CalculateDebtToEquity_ModeratelyHighLeverage_ReturnsWatchSeverity()
    {
        // 1,400,000 / 600,000 = 2.33
        var extraction = Build("Leveraged Growth Co.",
            Item(CanonicalLineItem.TotalLiabilities, 1_400_000m),
            Item(CanonicalLineItem.TotalEquity, 600_000m));

        var result = LeverageCalculator.CalculateDebtToEquity(extraction);

        Assert.Equal(2.33m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Watch, result.Flag.Severity);
    }

    [Fact]
    public void CalculateDebtToEquity_ExcessiveLeverage_ReturnsBreachSeverity()
    {
        // 2,000,000 / 600,000 = 3.33
        var extraction = Build("Overleveraged LLC",
            Item(CanonicalLineItem.TotalLiabilities, 2_000_000m),
            Item(CanonicalLineItem.TotalEquity, 600_000m));

        var result = LeverageCalculator.CalculateDebtToEquity(extraction);

        Assert.Equal(3.33m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Breach, result.Flag.Severity);
    }

    [Fact]
    public void CalculateDebtToEquity_ZeroEquity_ThrowsRatherThanDivideByZero()
    {
        var extraction = Build("Zero Equity Co.",
            Item(CanonicalLineItem.TotalLiabilities, 500_000m),
            Item(CanonicalLineItem.TotalEquity, 0m));

        Assert.Throws<RatioCalculationException>(() => LeverageCalculator.CalculateDebtToEquity(extraction));
    }
}
