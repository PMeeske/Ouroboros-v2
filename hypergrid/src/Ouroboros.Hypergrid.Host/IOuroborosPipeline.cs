namespace Ouroboros.Hypergrid.Host;

using Ouroboros.Abstractions.Core;

/// <summary>
/// Bridge abstraction representing the real Ouroboros engine's pipeline
/// capabilities. Extends <see cref="IChatCompletionModel"/> from the
/// foundation layer — any <c>IOuroborosPipeline</c> implementation
/// automatically satisfies <c>IChatCompletionModel</c> via the default
/// interface implementation of <see cref="IChatCompletionModel.GenerateTextAsync"/>.
/// </summary>
public interface IOuroborosPipeline : IChatCompletionModel
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

    /// <inheritdoc/>
    Task<string> IChatCompletionModel.GenerateTextAsync(string prompt, CancellationToken ct)
        => GenerateAsync(prompt, null, ct);
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
