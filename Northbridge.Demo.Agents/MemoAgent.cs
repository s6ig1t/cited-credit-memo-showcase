using System.Text.Json;
using Northbridge.Demo.Core;
using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Northbridge.Demo.Agents;

/// <summary>
/// Drafts the narrative sections of a credit memo from an already-computed FinancialSpread.
///
/// THIS IS THE ENFORCEMENT POINT OF THE PROJECT'S CENTRAL DESIGN RULE. This agent's prompt
/// never includes a single raw line item value, and it is never asked to compute anything.
/// It receives ratio names, already-rounded values, formula descriptions, and policy flags,
/// all produced deterministically by Northbridge.Demo.Analysis, and its only job is to write about
/// them. If you're explaining this project to someone, this is the class to point to.
///
/// CITATION HANDLING: rather than asking the model to reproduce citation data itself (page
/// numbers, snippets), which would mean trusting an LLM to accurately copy structured data
/// it was only shown as context, this agent asks the model to name WHICH ratios each section
/// discusses. The actual SourceCitation objects for those ratios are then looked up from the
/// already-known RatioResult.SupportingCitations in code, not reconstructed by the model.
/// The model chooses what to write about; it never gets a chance to misreport where a number
/// came from.
///
/// Builds its own AIAgent from the shared AnthropicClient, same pattern as ExtractionAgent,
/// but with its own distinct name and instructions, since this agent's job (drafting prose
/// from verified numbers) is deliberately different from extraction's job (reading a document).
/// </summary>
public sealed class MemoAgent
{
    private readonly AIAgent _agent;

    public MemoAgent(AnthropicClient anthropicClient, string model = "claude-sonnet-4-5")
    {
        _agent = anthropicClient.AsAIAgent(
            model: model,
            name: "CreditMemoDraftingAgent",
            instructions: SystemInstructions);
    }

    public async Task<CreditMemo> DraftAsync(FinancialSpread spread, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(spread);

        var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);

        var sections = ParseMemoResponse(response.Text, spread);

        return new CreditMemo(
            spread.Extraction.BorrowerName,
            DateTime.UtcNow,
            spread,
            sections);
    }

    private static string BuildPrompt(FinancialSpread spread)
    {
        // Deliberately only ratio-level facts go into this payload: name, value, the formula
        // used, and the policy flag. No ExtractedLineItem, no raw balance sheet figures.
        // The model cannot recompute or "correct" a ratio it was never shown the inputs for.
        var ratioFacts = spread.Ratios.Select(r => new
        {
            r.Name,
            r.Value,
            r.FormulaDescription,
            PolicySeverity = r.Flag.Severity.ToString(),
            PolicyMessage = r.Flag.Message
        });

        var payload = JsonSerializer.Serialize(new
        {
            borrowerName = spread.Extraction.BorrowerName,
            statementPeriod = spread.Extraction.StatementPeriodDescription,
            ratios = ratioFacts,
            dataLimitations = spread.CalculationWarnings
        });

        // Not using an interpolated raw string ($""") here on purpose: the prompt text below
        // also contains a literal JSON example with "{{" (a brace immediately followed by
        // another brace, for the nested "sections": [ { ... } ] shape). Interpolated raw
        // string literals need enough leading '$' characters to disambiguate "this is literal
        // text" from "this starts an interpolation", and mixing that counting with a large
        // block of example JSON is exactly the kind of thing that's easy to get wrong (it's
        // what caused a real compile error here on the first pass). Using a plain, non-
        // interpolated raw string with a simple placeholder token instead sidesteps the whole
        // problem: no brace-counting to get right, and the substitution point is unambiguous.
        const string payloadPlaceholder = "__FINANCIAL_DATA_PAYLOAD__";

        var promptTemplate = """
            Draft credit memo sections from the following already-calculated financial data.
            Do not recalculate, adjust, or second-guess any of these numbers; treat every
            value below as an established fact you are writing about, not something to verify.

            __FINANCIAL_DATA_PAYLOAD__

            Respond with ONLY a single JSON object, no markdown code fences, no commentary
            before or after it, matching exactly this shape:

            {
              "sections": [
                {
                  "heading": "string, e.g. 'Cash Flow and Debt Service Coverage'",
                  "narrativeText": "string, a few sentences of underwriting-style narrative",
                  "ratiosDiscussed": ["exact ratio Name strings from the input that this section references"]
                }
              ]
            }

            Write 2 to 4 sections. If dataLimitations is non-empty, include one section that
            plainly states what could not be evaluated due to missing source data, referencing
            no ratios for that section (ratiosDiscussed: []).
            """;

        return promptTemplate.Replace(payloadPlaceholder, payload);
    }

    private static IReadOnlyList<MemoSection> ParseMemoResponse(string rawResponseText, FinancialSpread spread)
    {
        var json = JsonResponseHelpers.ExtractJsonObject(rawResponseText);

        var dto = JsonSerializer.Deserialize<MemoResponseDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Memo Agent returned JSON that deserialized to null.");

        // Index ratios by name once, rather than scanning spread.Ratios per section.
        var ratiosByName = spread.Ratios.ToDictionary(r => r.Name, r => r);

        return dto.Sections
            .Select(sectionDto =>
            {
                var citations = sectionDto.RatiosDiscussed
                    .Where(ratiosByName.ContainsKey)
                    .SelectMany(name => ratiosByName[name].SupportingCitations)
                    .Distinct()
                    .ToList();

                return new MemoSection(sectionDto.Heading, sectionDto.NarrativeText, citations);
            })
            .ToList();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SystemInstructions = """
        You are a credit memo drafting assistant for a commercial lending platform. You write
        clear, professional underwriting narrative from financial data that has already been
        calculated and verified elsewhere. You never perform arithmetic yourself, never
        restate a number differently than it was given to you, and never speculate about
        figures you were not provided. Your job is exposition, not analysis: explain what the
        numbers mean for this borrower's creditworthiness in plain, professional language a
        credit committee would expect, while staying strictly faithful to the values you were
        given.
        """;
}

file sealed class MemoResponseDto
{
    public List<MemoSectionDto> Sections { get; set; } = new();
}

file sealed class MemoSectionDto
{
    public string Heading { get; set; } = "";
    public string NarrativeText { get; set; } = "";
    public List<string> RatiosDiscussed { get; set; } = new();
}
