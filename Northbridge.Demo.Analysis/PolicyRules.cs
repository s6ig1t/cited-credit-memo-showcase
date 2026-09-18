using Northbridge.Demo.Core;

namespace Northbridge.Demo.Analysis;

/// <summary>
/// Underwriting policy thresholds, and the logic that turns a raw computed ratio into a
/// <see cref="PolicyFlag"/>.
///
/// WHY THIS IS ITS OWN FILE, SEPARATE FROM THE RATIO CALCULATORS:
/// A ratio formula (how DSCR is computed) and a policy threshold (what DSCR value is
/// acceptable) are two different kinds of decisions that change for different reasons.
/// The formula for DSCR is close to universal; the threshold a lender requires is a business
/// policy that varies by institution, by loan type, and over time. Keeping thresholds in one
/// dedicated place means updating policy never means touching calculation code, and vice versa.
///
/// The specific numbers below are realistic, commonly-cited commercial lending benchmarks,
/// used here for demonstration. A real deployment would source these from an actual
/// institution's credit policy, likely configuration rather than a hardcoded constant.
/// </summary>
public static class PolicyRules
{
    public const decimal DscrBreachBelow = 1.15m;
    public const decimal DscrWatchBelow = 1.25m;

    public const decimal CurrentRatioBreachBelow = 1.00m;
    public const decimal CurrentRatioWatchBelow = 1.20m;

    public const decimal DebtToEquityBreachAbove = 3.00m;
    public const decimal DebtToEquityWatchAbove = 2.00m;

    public static PolicyFlag EvaluateDscr(decimal dscr) => dscr switch
    {
        < DscrBreachBelow => new PolicyFlag(
            PolicyFlagSeverity.Breach,
            $"DSCR of {dscr:0.00} is below the minimum policy threshold of {DscrBreachBelow:0.00}."),
        < DscrWatchBelow => new PolicyFlag(
            PolicyFlagSeverity.Watch,
            $"DSCR of {dscr:0.00} is above the hard minimum but below the {DscrWatchBelow:0.00} comfort threshold."),
        _ => new PolicyFlag(PolicyFlagSeverity.None, $"DSCR of {dscr:0.00} meets policy.")
    };

    public static PolicyFlag EvaluateCurrentRatio(decimal ratio) => ratio switch
    {
        < CurrentRatioBreachBelow => new PolicyFlag(
            PolicyFlagSeverity.Breach,
            $"Current ratio of {ratio:0.00} is below 1.00, indicating current liabilities exceed current assets."),
        < CurrentRatioWatchBelow => new PolicyFlag(
            PolicyFlagSeverity.Watch,
            $"Current ratio of {ratio:0.00} is thin relative to the {CurrentRatioWatchBelow:0.00} comfort threshold."),
        _ => new PolicyFlag(PolicyFlagSeverity.None, $"Current ratio of {ratio:0.00} meets policy.")
    };

    public static PolicyFlag EvaluateDebtToEquity(decimal ratio) => ratio switch
    {
        > DebtToEquityBreachAbove => new PolicyFlag(
            PolicyFlagSeverity.Breach,
            $"Debt-to-equity of {ratio:0.00} exceeds the maximum policy threshold of {DebtToEquityBreachAbove:0.00}."),
        > DebtToEquityWatchAbove => new PolicyFlag(
            PolicyFlagSeverity.Watch,
            $"Debt-to-equity of {ratio:0.00} is above the {DebtToEquityWatchAbove:0.00} comfort threshold."),
        _ => new PolicyFlag(PolicyFlagSeverity.None, $"Debt-to-equity of {ratio:0.00} meets policy.")
    };

    public static PolicyFlag EvaluateWorkingCapital(decimal amount) => amount switch
    {
        < 0 => new PolicyFlag(
            PolicyFlagSeverity.Breach,
            $"Working capital is negative ({amount:C0}); current liabilities exceed current assets."),
        _ => new PolicyFlag(PolicyFlagSeverity.None, $"Working capital of {amount:C0} is positive.")
    };
}
