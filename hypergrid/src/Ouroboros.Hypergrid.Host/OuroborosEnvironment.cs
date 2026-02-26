namespace Ouroboros.Hypergrid.Host;

using System.Runtime.CompilerServices;
using Ouroboros.Hypergrid.Iaret;

/// <summary>
/// Engine → Iaret direction.
///
/// Wraps an <see cref="IOuroborosPipeline"/> (the real Ouroboros engine's
/// chat model / tool-aware model) as an <see cref="IIaretEnvironment"/>,
/// so the convergent Iaret can use the full pipeline for each aspect's
/// reasoning operations.
///
/// Usage:
/// <code>
/// var pipeline = new LiteLLMChatModel(...);     // from Ouroboros.Providers
/// var env = new OuroborosEnvironment(pipeline);  // wraps it
/// var iaret = IaretConvergence.Create(env);      // aspects use the real LLM
/// </code>
/// </summary>
public sealed class OuroborosEnvironment : IIaretEnvironment
{
    private readonly IOuroborosPipeline _pipeline;

    public OuroborosEnvironment(IOuroborosPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public string Name => _pipeline.ModelName;

    public bool SupportsStreaming => _pipeline.SupportsStreaming;

    public async Task<string> ProcessAsync(string input, AspectContext context, CancellationToken ct = default)
    {
        // Build the prompt with conversation history if available
        var prompt = context.History.Count > 0
            ? FormatWithHistory(input, context)
            : input;

        // Use tool-aware path if available
        if (_pipeline is IToolAwarePipeline toolPipeline)
            return await toolPipeline.GenerateWithToolsAsync(prompt, context.SystemPrompt, ct);

        return await _pipeline.GenerateAsync(prompt, context.SystemPrompt, ct);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string input,
        AspectContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = context.History.Count > 0
            ? FormatWithHistory(input, context)
            : input;

        await foreach (var chunk in _pipeline.StreamAsync(prompt, context.SystemPrompt, ct))
        {
            yield return chunk;
        }
    }

    private static string FormatWithHistory(string currentInput, AspectContext context)
    {
        var parts = new List<string>(context.History.Count + 2)
        {
            "Previous context:"
        };

        foreach (var entry in context.History)
            parts.Add($"- {entry}");

        parts.Add($"\nCurrent input: {currentInput}");

        return string.Join("\n", parts);
    }
}
