namespace Northbridge.Demo.Agents;

/// <summary>
/// Small shared helper for pulling a JSON object out of raw model text. Both ExtractionAgent
/// and MemoAgent ask Claude to respond with strict JSON, and both need the same defensive
/// handling if the model wraps its answer in a markdown code fence anyway. Centralizing this
/// avoids the two agents drifting into two slightly different versions of the same logic.
/// </summary>
internal static class JsonResponseHelpers
{
    public static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start < 0 || end < start)
        {
            throw new InvalidOperationException(
                $"Agent response did not contain a recognizable JSON object. Raw response: {text}");
        }

        return text[start..(end + 1)];
    }
}
