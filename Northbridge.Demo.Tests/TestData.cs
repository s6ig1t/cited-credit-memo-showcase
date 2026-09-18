using Northbridge.Demo.Core;

namespace Northbridge.Demo.Tests;

/// <summary>
/// Deliberately NOT one big shared "sample borrower" fixture reused across every test.
/// Each test in this project builds exactly the line items it needs via <see cref="Item"/>
/// and <see cref="Build"/>. That's a small extra bit of typing per test, but it means anyone
/// reading a single test method can see everything relevant to that test's outcome right
/// there, without having to go trace through a shared fixture to understand what data is
/// actually in play. Given how few line items most of these tests need, the explicitness
/// is worth more than the reuse would save.
/// </summary>
internal static class TestData
{
    public const string Period = "FY2025";

    public static ExtractedLineItem Item(CanonicalLineItem canonical, decimal value, int page = 1, string? label = null, string? snippet = null) =>
        new(canonical, label ?? canonical.ToString(), value, Period, new SourceCitation(page, snippet));

    public static FinancialExtractionResult Build(string borrowerName, params ExtractedLineItem[] items) =>
        new(borrowerName, "Fiscal Year Ended December 31, 2025", Period, items);
}
