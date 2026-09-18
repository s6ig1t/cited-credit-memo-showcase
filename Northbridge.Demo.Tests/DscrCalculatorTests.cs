using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Xunit;
using static Northbridge.Demo.Tests.TestData;

namespace Northbridge.Demo.Tests;

public class DscrCalculatorTests
{
    [Fact]
    public void Calculate_WithStrongCoverage_ReturnsCorrectValueAndNoPolicyFlag()
    {
        // Numerator: 250,000 + 80,000 + 40,000 = 370,000
        // Denominator: 40,000 + 60,000 = 100,000
        // DSCR = 3.70, comfortably above the 1.25 comfort threshold
        //
        // Net Income, D&A, and Interest Expense all appear on the same page (2) here, which
        // is realistic: an income statement usually fits on one page. Each is given a distinct
        // snippet, the way a real extraction should, since SourceCitation is a record with
        // structural equality: two citations with the same page AND the same (null) snippet
        // are considered equal and get collapsed by Distinct(). Without distinguishing
        // snippets, three same-page figures with no snippet would deduplicate down to one
        // citation, which is why this test sets one explicitly for each.
        var extraction = Build("Example Fabrication LLC",
            Item(CanonicalLineItem.NetIncome, 250_000m, page: 2, snippet: "Net income: $250,000"),
            Item(CanonicalLineItem.DepreciationAndAmortization, 80_000m, page: 2, snippet: "Depreciation and amortization: $80,000"),
            Item(CanonicalLineItem.InterestExpense, 40_000m, page: 2, snippet: "Interest expense: $40,000"),
            Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 60_000m, page: 3, snippet: "Current portion of long-term debt: $60,000"));

        var result = DscrCalculator.Calculate(extraction);

        Assert.Equal(3.70m, result.Value);
        Assert.Equal(PolicyFlagSeverity.None, result.Flag.Severity);
        // With each source line item carrying a distinguishing snippet, all four citations
        // are now genuinely distinct, even though two of them share a page number. This is
        // the traceability guarantee, tested directly.
        Assert.Equal(4, result.SupportingCitations.Count);
    }

    [Fact]
    public void Calculate_WithThinCoverage_ReturnsWatchSeverity()
    {
        // Numerator: 90,000 + 10,000 + 20,000 = 120,000
        // Denominator: 20,000 + 80,000 = 100,000
        // DSCR = 1.20, inside the Watch band (>= 1.15 and < 1.25)
        var extraction = Build("Marginal Coverage Co.",
            Item(CanonicalLineItem.NetIncome, 90_000m),
            Item(CanonicalLineItem.DepreciationAndAmortization, 10_000m),
            Item(CanonicalLineItem.InterestExpense, 20_000m),
            Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 80_000m));

        var result = DscrCalculator.Calculate(extraction);

        Assert.Equal(1.20m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Watch, result.Flag.Severity);
    }

    [Fact]
    public void Calculate_WithWeakCoverage_ReturnsBreachSeverity()
    {
        // Numerator: 20,000 + 10,000 + 40,000 = 70,000
        // Denominator: 40,000 + 60,000 = 100,000
        // DSCR = 0.70, below the 1.15 hard minimum
        var extraction = Build("Stressed Borrower Inc.",
            Item(CanonicalLineItem.NetIncome, 20_000m),
            Item(CanonicalLineItem.DepreciationAndAmortization, 10_000m),
            Item(CanonicalLineItem.InterestExpense, 40_000m),
            Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 60_000m));

        var result = DscrCalculator.Calculate(extraction);

        Assert.Equal(0.70m, result.Value);
        Assert.Equal(PolicyFlagSeverity.Breach, result.Flag.Severity);
    }

    [Fact]
    public void Calculate_MissingRequiredLineItem_ThrowsWithClearMessage()
    {
        // Interest Expense is intentionally omitted here.
        var incompleteExtraction = Build("Incomplete Statement LLC",
            Item(CanonicalLineItem.NetIncome, 100_000m),
            Item(CanonicalLineItem.DepreciationAndAmortization, 20_000m),
            Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 50_000m));

        var ex = Assert.Throws<RatioCalculationException>(() => DscrCalculator.Calculate(incompleteExtraction));

        // The exception message is user-facing (it ends up in FinancialSpread.CalculationWarnings),
        // so asserting on its content isn't just testing plumbing, it's testing that a human
        // reviewer would actually understand what went wrong.
        Assert.Contains("InterestExpense", ex.Message);
        Assert.Contains(DscrCalculator.RatioName, ex.Message);
    }

    [Fact]
    public void Calculate_WithZeroTotalDebtService_ThrowsRatherThanDivideByZero()
    {
        var extraction = Build("Debt-Free Co.",
            Item(CanonicalLineItem.NetIncome, 100_000m),
            Item(CanonicalLineItem.DepreciationAndAmortization, 10_000m),
            Item(CanonicalLineItem.InterestExpense, 0m),
            Item(CanonicalLineItem.CurrentPortionOfLongTermDebt, 0m));

        Assert.Throws<RatioCalculationException>(() => DscrCalculator.Calculate(extraction));
    }
}
