using System.Text.Json;
using Northbridge.Demo.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Northbridge.Demo.Agents;

/// <summary>
/// Reads a financial statement PDF and returns a structured, canonically-mapped, cited
/// extraction. This is the ONLY place in the whole solution that asks an LLM to read a
/// source document. Everything downstream, ratio calculation (Northbridge.Demo.Analysis) and
/// memo drafting (MemoAgent), works from this agent's output, never from the PDF directly.
///
/// Takes an already-built AIAgent rather than a provider-specific client. Which provider
/// (Anthropic, OpenAI) actually backs that AIAgent is decided once, upstream, in Program.cs's
/// LlmAgentFactory; this class has no idea which one it got and does not need to. That is the
/// whole point of building against Microsoft.Agents.AI's AIAgent abstraction instead of coding
/// directly against AnthropicClient here: this class, and MemoAgent, stay provider-agnostic,
/// and a new provider can be added later by changing only the factory, not either agent class.
/// Each agent class still gets its own AIAgent instance, built with its own fixed instructions
/// (this one: extraction only, never analysis) and its own name, even though both instances
/// may ultimately be backed by the same underlying provider client.
///
/// The PDF is passed as a multimodal ChatMessage (TextContent + DataContent), the standard
/// Microsoft.Extensions.AI pattern AIAgent is built on. This has been run end to end against
/// a real financial statement PDF and confirmed working: Claude correctly extracted every
/// line item across two reporting periods, with accurate page numbers and snippets for each.
/// </summary>
public sealed class ExtractionAgent
{
    private readonly AIAgent _agent;

    public ExtractionAgent(AIAgent agent)
    {
        _agent = agent;
    }

    public async Task<FinancialExtractionResult> ExtractAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        // Checked before anything else in this method: no point building a request, or
        // spending a network round-trip, on a PDF we already know Claude's API will reject.
        // See PdfLimitGuard's own comments for exactly what is and isn't reliably enforced here.
        PdfLimitGuard.EnsureWithinLimits(pdfBytes);

        var message = new ChatMessage(ChatRole.User, new List<AIContent>
        {
            new TextContent(UserInstructions),
            new DataContent(pdfBytes, "application/pdf")
        });

        var response = await _agent.RunAsync(message, cancellationToken: cancellationToken);

        return ParseExtractionResponse(response.Text);
    }

    private static FinancialExtractionResult ParseExtractionResponse(string rawResponseText)
    {
        var json = ExtractJsonObject(rawResponseText);

        var dto = JsonSerializer.Deserialize<ExtractionResponseDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Extraction Agent returned JSON that deserialized to null.");

        var lineItems = dto.LineItems
            .Select(item => new ExtractedLineItem(
                Canonical: ParseCanonical(item.Canonical),
                LabelAsShownInDocument: item.LabelAsShownInDocument,
                Value: item.Value,
                Period: item.Period,
                Citation: new SourceCitation(item.PageNumber, item.Snippet)))
            // Anything the model couldn't confidently map to our fixed vocabulary comes back
            // as Unknown; we drop those here rather than passing meaningless line items into
            // the Analysis engine. A production version would likely log these for review
            // instead of silently discarding them.
            .Where(li => li.Canonical != CanonicalLineItem.Unknown)
            .ToList();

        return new FinancialExtractionResult(
            dto.BorrowerName,
            dto.StatementPeriodDescription,
            dto.PrimaryPeriod,
            lineItems);
    }

    private static CanonicalLineItem ParseCanonical(string value) =>
        Enum.TryParse<CanonicalLineItem>(value, ignoreCase: true, out var parsed) ? parsed : CanonicalLineItem.Unknown;

    private static string ExtractJsonObject(string text) => JsonResponseHelpers.ExtractJsonObject(text);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Fixed system instructions: this agent's behavior should not vary per call, so this is
    // a constant rather than something built per-request. Internal, not private, so
    // LlmAgentFactory (same assembly) can read it when building this agent's AIAgent; nothing
    // outside this assembly has any business seeing it.
    internal const string SystemInstructions = """
        You are a financial statement extraction specialist for a commercial lending platform.
        Your only job is to read a financial statement document and extract specific line items
        into a strict JSON format. You do not analyze, calculate ratios, or offer opinions on
        creditworthiness. You extract facts and report exactly where each fact came from.

        You must map each line item you find to one of these exact canonical names (case-sensitive):
        TotalRevenue, NetIncome, InterestExpense, DepreciationAndAmortization,
        TotalCurrentAssets, TotalCurrentLiabilities, TotalAssets, TotalLiabilities,
        TotalEquity, CashAndCashEquivalents, CurrentPortionOfLongTermDebt, TotalDebt

        If a figure on the page does not clearly correspond to one of these concepts, do not
        include it. Do not invent a canonical name that is not in this list.

        For every line item, report the page number it appears on and a short snippet (a few
        words) from that exact spot on the page, so the figure can be located again by a human
        reviewer. If a statement shows more than one reporting period side by side, extract
        line items for every period shown, and identify which period is the primary (most
        current) one.
        """;

    private const string UserInstructions = """
        Extract the financial statement attached to this message. Respond with ONLY a single
        JSON object, no markdown code fences, no commentary before or after it, matching
        exactly this shape:

        {
          "borrowerName": "string",
          "statementPeriodDescription": "string, e.g. 'Fiscal Year Ended December 31, 2025'",
          "primaryPeriod": "string, e.g. 'FY2025'",
          "lineItems": [
            {
              "canonical": "one of the fixed canonical names",
              "labelAsShownInDocument": "the literal label as printed on the statement",
              "value": 0.00,
              "period": "string, e.g. 'FY2025'",
              "pageNumber": 1,
              "snippet": "a few words from that spot on the page"
            }
          ]
        }
        """;
}

/// <summary>
/// Plain deserialization targets for the Extraction Agent's JSON response. Kept private to
/// this file since nothing outside ExtractionAgent should ever construct or depend on these;
/// the public contract of this class is FinancialExtractionResult (from Northbridge.Demo.Core),
/// not this wire-shape DTO.
/// </summary>
file sealed class ExtractionResponseDto
{
    public string BorrowerName { get; set; } = "";
    public string StatementPeriodDescription { get; set; } = "";
    public string PrimaryPeriod { get; set; } = "";
    public List<ExtractionLineItemDto> LineItems { get; set; } = new();
}

file sealed class ExtractionLineItemDto
{
    public string Canonical { get; set; } = "";
    public string LabelAsShownInDocument { get; set; } = "";
    public decimal Value { get; set; }
    public string Period { get; set; } = "";
    public int PageNumber { get; set; }
    public string? Snippet { get; set; }
}
