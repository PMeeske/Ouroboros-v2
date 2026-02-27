namespace Ouroboros.Hypergrid.Iaret;

/// <summary>
/// The entry point for an entire Iaret node's capabilities.
/// Wraps whatever underlying system powers the node — the real Ouroboros
/// pipeline (IChatModel, tools, RAG, etc.) or a local standalone fallback.
///
/// Pass one of these per node to <see cref="IaretConvergence.Create"/>.
/// Each aspect receives it and uses it for its Transform operations.
///
/// To integrate with the real CLI/Engine:
/// <code>
/// class OuroborosEnvironment : IIaretEnvironment
/// {
///     private readonly IChatModel _llm;
///     public OuroborosEnvironment(IChatModel llm, ...) { ... }
///     public Task&lt;string&gt; ProcessAsync(string input, AspectContext ctx, CancellationToken ct)
///         => _llm.GenerateAsync(new ChatMessage(ctx.SystemPrompt, input));
/// }
/// </code>
/// </summary>
public interface IIaretEnvironment
{
    /// <summary>
    /// Process an input through this environment's underlying capabilities.
    /// This is the single method each aspect calls to do its real work.
    /// </summary>
    /// <param name="input">The raw thought payload to process.</param>
    /// <param name="context">Aspect-specific context (system prompt, config, history).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The processed output string.</returns>
    Task<string> ProcessAsync(string input, AspectContext context, CancellationToken ct = default);

    /// <summary>
    /// Whether this environment supports streaming responses.
    /// When true, aspects may use <see cref="StreamAsync"/> for incremental output.
    /// </summary>
    bool SupportsStreaming => false;

    /// <summary>
    /// Stream a response incrementally. Default throws NotSupportedException.
    /// Override when backed by a streaming LLM provider.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(string input, AspectContext context, CancellationToken ct = default) =>
        throw new NotSupportedException($"{GetType().Name} does not support streaming.");

    /// <summary>Human-readable name of this environment (e.g., "Ollama/llama3", "Local", "Anthropic/Claude").</summary>
    string Name { get; }
}

/// <summary>
/// Context passed from an aspect to the environment on each call.
/// Carries the aspect's identity, system prompt, and any accumulated state.
/// </summary>
public sealed record AspectContext
{
    /// <summary>Which aspect is making the call (e.g., "analytical", "creative").</summary>
    public required string AspectId { get; init; }

    /// <summary>The aspect's system prompt / persona instruction.</summary>
    public required string SystemPrompt { get; init; }

    /// <summary>Recent conversation history for this aspect (sliding window).</summary>
    public IReadOnlyList<string> History { get; init; } = [];

    /// <summary>Optional key-value parameters the aspect wants to pass through.</summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
