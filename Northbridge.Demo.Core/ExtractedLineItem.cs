namespace Northbridge.Demo.Core;

/// <summary>
/// A single financial figure as extracted from the source document, tied both to a
/// canonical concept (for calculation) and to its original wording and location (for
/// traceability).
///
/// WHY BOTH LabelAsShownInDocument AND Canonical:
/// Keeping the original label is what lets a human reviewer trust the mapping. If Claude
/// maps "Profit attributable to shareholders" to CanonicalLineItem.NetIncome, a reviewer
/// can see exactly what judgment call was made and challenge it if it looks wrong. Throwing
/// away the original wording in favor of only the canonical value would make the extraction
/// step a black box, which is precisely what an auditable lending pipeline cannot afford.
/// </summary>
/// <param name="Canonical">The fixed vocabulary concept this line item represents.</param>
/// <param name="LabelAsShownInDocument">The literal label as it appeared on the source page.</param>
/// <param name="Value">The numeric value, in the document's stated currency and units.</param>
/// <param name="Period">
/// A short description of the reporting period this figure applies to, e.g. "FY2025" or
/// "Q3 2025". Financial statements often show multiple periods side by side (current year,
/// prior year), so this disambiguates which column a value came from.
/// </param>
/// <param name="Citation">Where in the source document this value was found.</param>
public record ExtractedLineItem(
    CanonicalLineItem Canonical,
    string LabelAsShownInDocument,
    decimal Value,
    string Period,
    SourceCitation Citation);
