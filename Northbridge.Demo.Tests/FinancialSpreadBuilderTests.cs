using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Xunit;
using static Northbridge.Demo.Tests.TestData;

namespace Northbridge.Demo.Tests;

public class FinancialSpreadBuilderTests
{
    private static FinancialExtractionResult CompleteHealthyExtraction() => Build(
        "Example Fabrication LLC",
        Item(CanonicalLineItem.NetIncome, 250_000m, page: 2),
        Item(CanonicalLineItem.DepreciationAndAmortization, 80_000m, page: 2),
        Item(CanonicalLineItem.InterestExpense, 40_000m, page: 2),
        Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 60_000m, page: 3),
        Item(CanonicalLineItem.TotalCurrentAssets, 500_000m, page: 3),
        Item(CanonicalLineItem.TotalCurrentLiabilities, 300_000m, page: 3),
        Item(CanonicalLineItem.TotalLiabilities, 900_000m, page: 3),
        Item(CanonicalLineItem.TotalEquity, 600_000m, page: 3));

    [Fact]
    public void Build_WithCompleteData_ComputesAllFourRatiosAndNoWarnings()
    {
        var spread = FinancialSpreadBuilder.Build(CompleteHealthyExtraction());

        Assert.Equal(4, spread.Ratios.Count);
        Assert.Empty(spread.CalculationWarnings);
    }

    [Fact]
    public void Build_MissingOneIncomeStatementFigure_SkipsOnlyDscrAndRecordsWarning()
    {
        // Remove Interest Expense: this should take out DSCR specifically. Current Ratio,
        // Working Capital, and Debt-to-Equity don't depend on it at all, so they should
        // still compute normally. This is the core "graceful degradation" guarantee,
        // tested directly rather than just asserted in a comment.
        var full = CompleteHealthyExtraction();
        var incomplete = full with
        {
            LineItems = full.LineItems.Where(li => li.Canonical != CanonicalLineItem.InterestExpense).ToList()
        };

        var spread = FinancialSpreadBuilder.Build(incomplete);

        Assert.Equal(3, spread.Ratios.Count);
        Assert.DoesNotContain(spread.Ratios, r => r.Name == DscrCalculator.RatioName);
        Assert.Contains(spread.Ratios, r => r.Name == LiquidityCalculator.CurrentRatioName);
        Assert.Contains(spread.Ratios, r => r.Name == LiquidityCalculator.WorkingCapitalName);
        Assert.Contains(spread.Ratios, r => r.Name == LeverageCalculator.RatioName);

        var warning = Assert.Single(spread.CalculationWarnings);
        Assert.Contains(DscrCalculator.RatioName, warning);
        Assert.Contains("InterestExpense", warning);
    }

    [Fact]
    public void Build_WithNoLineItemsAtAll_ReturnsEmptySpreadWithFourWarnings_NotAnException()
    {
        // The extreme case: an extraction that found nothing usable at all. The builder
        // should still hand back a valid, if empty, FinancialSpread rather than throwing,
        // since a caller further up the pipeline (eventually the Memo Agent) needs
        // something to work with even when the news is "we couldn't calculate anything."
        var empty = Build("Unreadable Statement LLC");

        var spread = FinancialSpreadBuilder.Build(empty);

        Assert.Empty(spread.Ratios);
        Assert.Equal(4, spread.CalculationWarnings.Count);
    }
}
