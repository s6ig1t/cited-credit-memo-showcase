using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Xunit;

namespace Northbridge.Demo.Tests;

/// <summary>
/// These tests exist specifically to pin down behavior AT the threshold boundaries
/// (exactly 1.15, exactly 1.25, and so on), not just comfortably inside or outside each
/// band. A policy threshold is the one place in this whole project where an off-by-one
/// mistake (using &lt;= instead of &lt;, for instance) has real consequences: it's the
/// difference between correctly flagging a borrower right at the edge of policy and
/// silently waving them through, or vice versa. Boundary values are exactly where that
/// kind of bug hides, so they're worth testing explicitly rather than trusting that
/// "comfortably inside the band" tests would have caught it.
/// </summary>
public class PolicyRulesTests
{
    [Theory]
    [InlineData(1.14, PolicyFlagSeverity.Breach)] // just below the hard minimum
    [InlineData(1.15, PolicyFlagSeverity.Watch)]  // exactly at the hard minimum: passes it, still thin
    [InlineData(1.24, PolicyFlagSeverity.Watch)]  // just below the comfort threshold
    [InlineData(1.25, PolicyFlagSeverity.None)]   // exactly at the comfort threshold: meets policy
    public void EvaluateDscr_AtBoundaries_ReturnsExpectedSeverity(decimal dscr, PolicyFlagSeverity expected)
    {
        var flag = PolicyRules.EvaluateDscr(dscr);
        Assert.Equal(expected, flag.Severity);
    }

    [Theory]
    [InlineData(0.99, PolicyFlagSeverity.Breach)]
    [InlineData(1.00, PolicyFlagSeverity.Watch)]
    [InlineData(1.19, PolicyFlagSeverity.Watch)]
    [InlineData(1.20, PolicyFlagSeverity.None)]
    public void EvaluateCurrentRatio_AtBoundaries_ReturnsExpectedSeverity(decimal ratio, PolicyFlagSeverity expected)
    {
        var flag = PolicyRules.EvaluateCurrentRatio(ratio);
        Assert.Equal(expected, flag.Severity);
    }

    [Theory]
    [InlineData(2.99, PolicyFlagSeverity.Watch)]
    [InlineData(3.00, PolicyFlagSeverity.Watch)]  // exactly AT the breach threshold does not itself breach; only exceeding it does
    [InlineData(3.01, PolicyFlagSeverity.Breach)]
    [InlineData(2.00, PolicyFlagSeverity.None)]   // exactly AT the watch threshold does not itself trigger watch
    [InlineData(2.01, PolicyFlagSeverity.Watch)]
    public void EvaluateDebtToEquity_AtBoundaries_ReturnsExpectedSeverity(decimal ratio, PolicyFlagSeverity expected)
    {
        var flag = PolicyRules.EvaluateDebtToEquity(ratio);
        Assert.Equal(expected, flag.Severity);
    }

    [Theory]
    [InlineData(-0.01, PolicyFlagSeverity.Breach)]
    [InlineData(0, PolicyFlagSeverity.None)]  // exactly zero is not negative; treated as acceptable
    [InlineData(0.01, PolicyFlagSeverity.None)]
    public void EvaluateWorkingCapital_AtBoundaries_ReturnsExpectedSeverity(decimal amount, PolicyFlagSeverity expected)
    {
        var flag = PolicyRules.EvaluateWorkingCapital(amount);
        Assert.Equal(expected, flag.Severity);
    }
}
