namespace Ouroboros.Hypergrid.Host;

using System.Runtime.CompilerServices;
using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Iaret → Engine direction (the reciprocal).
///
/// Wraps an <see cref="IaretConvergence"/> as an <see cref="IOuroborosPipeline"/>,
/// so the CLI host or any pipeline consumer can use the convergent Iaret identity
/// as if it were a chat model. Every <c>GenerateAsync</c> call runs a full
/// convergence cycle: fan-out to aspects, simulation, synthesis.
///
/// This is what makes the hosting reciprocal — the same CLI host passes the
/// real LLM into Iaret (via <see cref="OuroborosEnvironment"/>), and exposes
/// the convergent output back as a pipeline endpoint.
///
/// Usage:
/// <code>
/// var iaret = IaretConvergence.Create(env);
/// var asModel = new IaretPipelineAdapter(iaret);
/// // Now anything expecting IOuroborosPipeline can use convergent Iaret
/// string response = await asModel.GenerateAsync("What is consciousness?");
/// </code>
/// </summary>
public sealed class IaretPipelineAdapter : IOuroborosPipeline, IDisposable
{
    private readonly IaretConvergence _convergence;

    public IaretPipelineAdapter(IaretConvergence convergence)
    {
        _convergence = convergence ?? throw new ArgumentNullException(nameof(convergence));
    }

    public string ModelName => $"Iaret/{_convergence.ComputeBackend}";

    public bool SupportsStreaming => true;

    public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
    {
        // If a system prompt is given, prepend it as context for the convergence
        var input = systemPrompt is not null
            ? $"[System: {systemPrompt}]\n{prompt}"
            : prompt;

        var thought = await _convergence.Think(input, ct);
        return thought.Payload;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Stream each sentence/segment of the convergent output
        var result = await GenerateAsync(prompt, systemPrompt, ct);

        // Yield the full response — convergence is inherently batch,
        // but we honor the streaming contract for pipeline compatibility.
        // For true streaming, use ThinkStream with a thought sequence.
        yield return result;
    }

    /// <summary>
    /// Directly query a specific sub-entity aspect through the pipeline interface.
    /// Returns the aspect's individual (non-converged) response.
    /// </summary>
    public async Task<string> QueryAspectAsync(string aspectId, string prompt, CancellationToken ct = default)
    {
        var thought = await _convergence.QueryAspectAsync(aspectId, prompt, ct);
        return thought.Payload;
    }

    /// <summary>
    /// Stream a sequence of prompts through the convergent identity.
    /// Each prompt runs a full convergence cycle.
    /// </summary>
    public async IAsyncEnumerable<string> GenerateStreamAsync(
        IAsyncEnumerable<string> prompts,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var origin = new GridCoordinate(0, 0, 0);

        var thoughtStream = ToThoughtStream(prompts, systemPrompt, origin, ct);

        await foreach (var result in _convergence.ThinkStream(thoughtStream, ct))
        {
            yield return result.Payload;
        }
    }

    private static async IAsyncEnumerable<Thought<string>> ToThoughtStream(
        IAsyncEnumerable<string> prompts,
        string? systemPrompt,
        GridCoordinate origin,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var prompt in prompts.WithCancellation(ct))
        {
            var input = systemPrompt is not null
                ? $"[System: {systemPrompt}]\n{prompt}"
                : prompt;

            yield return new Thought<string>
            {
                Payload = input,
                Origin = origin,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public void Dispose() => _convergence.Dispose();
}
