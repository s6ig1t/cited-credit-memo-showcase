namespace Northbridge.Demo.Core;

/// <summary>
/// How seriously a computed ratio deviates from an underwriting policy threshold.
/// </summary>
public enum PolicyFlagSeverity
{
    /// <summary>Within policy. No action needed.</summary>
    None,

    /// <summary>Close to a threshold; worth a human underwriter's attention, not a hard stop.</summary>
    Watch,

    /// <summary>Outside the policy threshold; requires explicit sign-off or an exception memo.</summary>
    Breach
}

/// <summary>
/// The result of checking one computed ratio against a lending policy rule.
///
/// WHY THIS IS SEPARATE FROM RatioResult ITSELF:
/// A ratio's value is a fact (DSCR is 1.15). Whether that fact is acceptable is a policy
/// judgment (this lender requires DSCR >= 1.25, so 1.15 is a Breach). Keeping these as two
/// steps mirrors how underwriting actually works, and it means the policy thresholds live
/// in one obvious place (see Northbridge.Demo.Analysis.PolicyRules) rather than being scattered
/// through calculation code.
/// </summary>
/// <param name="Severity">How serious the deviation is, if any.</param>
/// <param name="Message">A short, human-readable explanation of the check that was applied.</param>
public record PolicyFlag(PolicyFlagSeverity Severity, string Message);
