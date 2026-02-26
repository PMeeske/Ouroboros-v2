namespace Ouroboros.Hypergrid.Host;

/// <summary>
/// Minimal bridge abstraction representing the real Ouroboros engine's
/// pipeline capabilities. Mirrors what <c>IChatModel</c> / <c>ToolAwareChatModel</c>
/// provide in <c>Ouroboros.Core</c> and <c>Ouroboros.Providers</c>.
///
/// The real engine implements this directly. When the foundation/engine
/// submodules are present, <c>IOuroboros</c> or <c>IChatModel</c> either
/// extends this interface or is trivially adapted.
///
/// This keeps the hypergrid compilable independently of the engine submodule
/// while defining the exact contract needed for reciprocal hosting.
/// </summary>
public interface IOuroborosPipeline
{
    /// <summary>
    /// Send a prompt with an optional system instruction and get a response.
    /// This is the fundamental operation: one turn of the pipeline.
    /// </summary>
    Task<string> GenerateAsync(
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default);

    /// <summary>
    /// Stream a response token by token.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default);

    /// <summary>Whether this pipeline supports streaming output.</summary>
    bool SupportsStreaming { get; }

    /// <summary>Human-readable identifier (e.g., "LiteLLM/gpt-oss-120b", "Ollama/llama3").</summary>
    string ModelName { get; }
}

/// <summary>
/// Extended pipeline interface supporting tool use and multi-turn conversations.
/// Mirrors <c>ToolAwareChatModel</c> from <c>Ouroboros.Tools</c>.
/// </summary>
public interface IToolAwarePipeline : IOuroborosPipeline
{
    /// <summary>
    /// Generate a response with tool use enabled.
    /// The pipeline handles tool invocation and response integration internally.
    /// </summary>
    Task<string> GenerateWithToolsAsync(
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default);
}

/// <summary>
/// Multi-turn conversation context for pipelines that maintain state.
/// </summary>
public sealed record ConversationTurn(string Role, string Content)
{
    public static ConversationTurn System(string content) => new("system", content);
    public static ConversationTurn User(string content) => new("user", content);
    public static ConversationTurn Assistant(string content) => new("assistant", content);
}
