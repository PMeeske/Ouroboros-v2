namespace Ouroboros.Hypergrid.Iaret;

/// <summary>
/// Standalone local environment — no LLM, no API keys, no network.
/// Delegates processing back to the aspect's own local transform logic
/// by returning the input unchanged. Each aspect applies its own heuristic
/// transforms on top. This is the fallback when the real pipeline isn't available.
/// </summary>
public sealed class LocalIaretEnvironment : IIaretEnvironment
{
    public string Name => "Local";

    public Task<string> ProcessAsync(string input, AspectContext context, CancellationToken ct = default)
    {
        // Local environment: return input as-is.
        // The aspect's Transform method applies its own local logic.
        return Task.FromResult(input);
    }
}
