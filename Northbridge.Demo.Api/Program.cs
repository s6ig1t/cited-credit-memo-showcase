using Northbridge.Demo.Agents;
using Northbridge.Demo.Analysis;
using Northbridge.Demo.Core;
using Anthropic;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// User secrets is how the Anthropic API key gets into this app locally: never hardcoded,
// never in appsettings.json (which would get committed to source control). In a real
// company deployment, this line would be replaced by reading from a secrets manager (AWS
// Secrets Manager, Azure Key Vault) or an environment variable injected by CI/CD at deploy
// time, exactly the distinction between personal and production credential handling this
// project is meant to demonstrate understanding of.
builder.Configuration.AddUserSecrets<Program>();

// Registered with an EXPLICIT type parameter on purpose, not just AddSingleton(sp => ...).
// A factory lambda's return type is inferred from its compile-time expression type, which
// can silently differ from the type you actually meant to register under, especially with
// object initializers or wrapper methods. Being explicit here removes any ambiguity about
// what type this is registered as, and costs nothing.
builder.Services.AddSingleton<AnthropicClient>(sp =>
{
    var apiKey = builder.Configuration["AnthropicApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "AnthropicApiKey is not set. Right-click Northbridge.Demo.Api in Visual Studio, choose " +
            "'Manage User Secrets', and add: { \"AnthropicApiKey\": \"sk-ant-your-key-here\" }");
    }

    return new AnthropicClient { ApiKey = apiKey };
});

builder.Services.AddSingleton<ExtractionAgent>(sp => new ExtractionAgent(sp.GetRequiredService<AnthropicClient>()));
builder.Services.AddSingleton<MemoAgent>(sp => new MemoAgent(sp.GetRequiredService<AnthropicClient>()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Northbridge Demo - Credit Memo Pipeline",
        Version = "v1",
        Description = "Upload a financial statement PDF; get back a cited, ratio-backed credit memo draft."
    });
});

// Uploaded statements are only ever a handful of pages, but Kestrel's default multipart
// body size limit is conservative enough to reject a real scanned PDF. Raised explicitly
// rather than leaving a real-world file to fail with a confusing 413 error.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024; // 25 MB
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serves the HTML memo viewer from wwwroot once it exists (next milestone). Harmless with
// an empty wwwroot in the meantime.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.MapPost("/api/credit-memo", async (
        IFormFile file,
        ExtractionAgent extractionAgent,
        MemoAgent memoAgent,
        CancellationToken cancellationToken) =>
    {
        if (file.Length == 0)
        {
            return Results.BadRequest("No file was uploaded.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only PDF files are supported.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var pdfBytes = memoryStream.ToArray();

        FinancialExtractionResult extraction;
        try
        {
            // Stage 1: read the source document. This is the only stage that touches the PDF
            // or asks an LLM to read anything; its output is a structured, cited extraction.
            // ExtractAsync runs the PdfLimitGuard check first, so an oversized file is caught
            // here as a specific, actionable 400, not a generic failure from deep inside the
            // pipeline or a raw error from Claude's API.
            extraction = await extractionAgent.ExtractAsync(pdfBytes, cancellationToken);
        }
        catch (PdfTooLargeException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        // Stage 2: deterministic arithmetic, zero LLM involvement. Ratios, policy flags,
        // and graceful handling of any missing source data all happen here.
        var spread = FinancialSpreadBuilder.Build(extraction);

        // Stage 3: narrative drafting from already-computed, already-correct numbers only.
        // The Memo Agent never sees a raw line item, only the finished ratios from Stage 2.
        var memo = await memoAgent.DraftAsync(spread, cancellationToken);

        return Results.Ok(memo);
    })
    .WithName("GenerateCreditMemo")
    .DisableAntiforgery(); // Needed for IFormFile binding on a minimal API endpoint.

app.Run();

// Required so builder.Configuration.AddUserSecrets<Program>() and Microsoft.NET.Test.Sdk-style
// discovery have a concrete type to anchor to; top-level statement programs need this explicit
// partial class declaration to be referenced by name elsewhere (here, by AddUserSecrets<Program>()).
public partial class Program { }
