using Northbridge.Demo.Core;
using Xunit;

namespace Northbridge.Demo.Tests;

/// <summary>
/// This class exists because of a real bug we caught before it shipped: FinancialExtractionResult.Find()
/// originally returned whichever matching line item came first in the list, with no regard
/// for which reporting period it belonged to. Since financial statements routinely show two
/// periods side by side, that would have silently returned the wrong year's figure the
/// moment real (non-hand-built) multi-period data flowed through the pipeline.
///
/// These tests pin down the fix directly, so if this behavior ever regresses, it fails loudly
/// here instead of surfacing later as a mysteriously wrong ratio.
/// </summary>
public class FinancialExtractionResultTests
{
    [Fact]
    public void Find_WithTwoPeriodsPresent_ReturnsPrimaryPeriodValueNotFirstInList()
    {
        var priorYearNetIncome = new ExtractedLineItem(
            CanonicalLineItem.NetIncome, "Net income", 180_000m, "FY2024", new SourceCitation(2));
        var currentYearNetIncome = new ExtractedLineItem(
            CanonicalLineItem.NetIncome, "Net income", 250_000m, "FY2025", new SourceCitation(2));

        // Prior year is listed FIRST here, deliberately, to prove Find() is not just
        // returning "whichever one appears first" (the old, buggy behavior).
        var extraction = new FinancialExtractionResult(
            "Example Fabrication LLC",
            "Comparative Statements, FY2025 and FY2024",
            PrimaryPeriod: "FY2025",
            LineItems: new[] { priorYearNetIncome, currentYearNetIncome });

        var found = extraction.Find(CanonicalLineItem.NetIncome);

        Assert.NotNull(found);
        Assert.Equal(250_000m, found!.Value);
        Assert.Equal("FY2025", found.Period);
    }

    [Fact]
    public void Find_WithExplicitPeriod_ReturnsThatPeriodRegardlessOfPrimaryPeriod()
    {
        var priorYearNetIncome = new ExtractedLineItem(
            CanonicalLineItem.NetIncome, "Net income", 180_000m, "FY2024", new SourceCitation(2));
        var currentYearNetIncome = new ExtractedLineItem(
            CanonicalLineItem.NetIncome, "Net income", 250_000m, "FY2025", new SourceCitation(2));

        var extraction = new FinancialExtractionResult(
            "Example Fabrication LLC",
            "Comparative Statements, FY2025 and FY2024",
            PrimaryPeriod: "FY2025",
            LineItems: new[] { priorYearNetIncome, currentYearNetIncome });

        // Explicitly asking for the prior year, even though it's not the primary period,
        // should still be possible: this is what a future year-over-year trend feature
        // would rely on.
        var found = extraction.Find(CanonicalLineItem.NetIncome, "FY2024");

        Assert.NotNull(found);
        Assert.Equal(180_000m, found!.Value);
    }

    [Fact]
    public void Find_WhenCanonicalItemMissingEntirely_ReturnsNullRatherThanThrowing()
    {
        var extraction = new FinancialExtractionResult(
            "Sparse Statement LLC",
            "Fiscal Year Ended December 31, 2025",
            PrimaryPeriod: "FY2025",
            LineItems: Array.Empty<ExtractedLineItem>());

        var found = extraction.Find(CanonicalLineItem.NetIncome);

        // Find() itself stays a simple, safe lookup; turning "missing" into a loud failure
        // is RequiredLineItem's job (in Northbridge.Demo.Analysis), not this type's job. Keeping
        // that responsibility out of Core is intentional, since Core has no concept of
        // "required for what calculation."
        Assert.Null(found);
    }
}
