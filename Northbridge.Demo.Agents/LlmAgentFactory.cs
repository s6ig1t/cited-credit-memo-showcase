using Anthropic;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

namespace Northbridge.Demo.Agents;

/// <summary>
/// Which LLM provider backs the agents in this pipeline, selected once at startup from
/// configuration (Llm:Provider in appsettings.json), not hardcoded to Anthropic.
/// </summary>
public enum LlmProvider
{
    Anthropic,
    OpenAI
}

/// <summary>
/// The one place in this solution that knows both providers exist. Everything downstream of
/// this class, ExtractionAgent, MemoAgent, the pipeline in Program.cs, works against the
/// provider-neutral AIAgent type from Microsoft.Agents.AI and never sees an AnthropicClient or
/// an OpenAIClient directly. That separation is what makes swapping or adding a provider a
/// change confined to this one file, rather than a change to every agent class that uses one.
///
/// Both provider SDKs are referenced unconditionally at compile time, here and in this
/// project's .csproj. There is no conditional "using", C# using directives are resolved at
/// compile time, not runtime, so both packages are always part of the build; which one
/// actually gets exercised is decided by an ordinary runtime branch below, over a plain
/// configuration value.
///
/// Each call builds a fresh AIAgent rather than caching one per provider, since this factory
/// is only ever invoked twice at startup (once for extraction, once for memo drafting), each
/// time with different fixed instructions baked in. That is not worth optimizing.
/// </summary>
public static class LlmAgentFactory
{
    // Two role-specific methods, rather than one generic CreateAgent(name, instructions) called
    // from Program.cs, on purpose. ExtractionAgent.SystemInstructions and MemoAgent.
    // SystemInstructions are `internal`, visible only inside this assembly; Program.cs lives in
    // the Api project, a separate assembly, so it has no business reading either constant
    // directly, and shouldn't have to know either agent's fixed name or instructions text to
    // wire one up. This factory is the only thing that needs to know both, since it lives in
    // the same assembly as the two agent classes.
    public static AIAgent CreateExtractionAgent(LlmProvider provider, string apiKey, string? model = null) =>
        BuildAgent(provider, apiKey, "FinancialExtractionAgent", ExtractionAgent.SystemInstructions, model);

    public static AIAgent CreateMemoAgent(LlmProvider provider, string apiKey, string? model = null) =>
        BuildAgent(provider, apiKey, "CreditMemoDraftingAgent", MemoAgent.SystemInstructions, model);

    private static AIAgent BuildAgent(
        LlmProvider provider,
        string apiKey,
        string name,
        string instructions,
        string? model)
    {
        return provider switch
        {
            LlmProvider.Anthropic => new AnthropicClient { ApiKey = apiKey }
                .AsAIAgent(
                    model: model ?? "claude-sonnet-4-5",
                    name: name,
                    instructions: instructions),

            // Microsoft.Agents.AI.OpenAI is a preview package at the time of writing, same as
            // Microsoft.Agents.AI.Anthropic was when this project started. The exact client
            // construction and extension method surface below is this project's best-effort
            // guess at the package's actual shape; it follows the same GetChatClient(model)
            // pattern the official OpenAI .NET SDK uses elsewhere, and the same .AsAIAgent(...)
            // extension shape the Anthropic connector uses. Treat this branch as unverified
            // until it has actually been built against the installed package in Visual Studio,
            // exactly like every other preview-package integration in this project.
            LlmProvider.OpenAI => new OpenAIClient(apiKey)
                .GetChatClient(model ?? "gpt-4o")
                .AsAIAgent(
                    name: name,
                    instructions: instructions),

            _ => throw new ArgumentOutOfRangeException(
                nameof(provider), provider, "Unrecognized LLM provider.")
        };
    }
}
