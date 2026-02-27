namespace Ouroboros.Hypergrid.Iaret;

using System.Runtime.CompilerServices;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;

/// <summary>
/// A single interactable sub-entity of the convergent Iaret identity.
/// Each aspect occupies a position in the Hypergrid and processes thought
/// streams through its specialized lens (analytical, creative, guardian, etc.).
///
/// Each aspect holds a reference to its <see cref="IIaretEnvironment"/>,
/// which is the entry point to the whole node's capabilities — the real
/// Ouroboros pipeline when available, or a local fallback otherwise.
///
/// Aspects are individually addressable — you can query, observe, or influence
/// a specific aspect — but their outputs converge into the unified Iaret identity.
/// </summary>
public abstract class IaretAspect : IGridCell<string, string>
{
    /// <summary>Unique identifier for this aspect (e.g., "analytical", "creative").</summary>
    public string AspectId { get; }

    /// <summary>Human-readable name (e.g., "The Analytical Eye").</summary>
    public string Name { get; }

    /// <summary>The dimensional axis this aspect primarily operates along.</summary>
    public int PrimaryDimension { get; }

    /// <summary>Current activation level of this aspect (0.0 = dormant, 1.0 = fully active).</summary>
    public double Activation { get; private set; }

    /// <summary>Number of thoughts this aspect has processed.</summary>
    public long ProcessedCount { get; private set; }

    /// <summary>The environment powering this node. Set via <see cref="Bind"/>.</summary>
    public IIaretEnvironment Environment { get; private set; }

    /// <summary>The system prompt this aspect sends to the environment.</summary>
    protected abstract string SystemPrompt { get; }

    protected IaretAspect(string aspectId, string name, int primaryDimension)
    {
        AspectId = aspectId ?? throw new ArgumentNullException(nameof(aspectId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PrimaryDimension = primaryDimension;
        Environment = new LocalIaretEnvironment();
    }

    /// <summary>
    /// Binds this aspect to an environment. Called by the convergence orchestrator
    /// to inject the node's capabilities into each aspect.
    /// </summary>
    public void Bind(IIaretEnvironment environment)
    {
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// The core reasoning operation. When backed by a real environment, calls through
    /// to the LLM/pipeline. When local, applies heuristic transforms.
    /// </summary>
    protected abstract Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct);

    /// <summary>
    /// Synchronous local-only transform. Used as the fallback when the environment
    /// is <see cref="LocalIaretEnvironment"/>. Override this in each aspect for
    /// the standalone heuristic logic.
    /// </summary>
    protected virtual string TransformLocal(string input, GridCoordinate position) => input;

    /// <summary>
    /// Calls the environment with this aspect's system prompt and context.
    /// Aspects should call this from <see cref="TransformAsync"/> to use the real pipeline.
    /// </summary>
    protected async Task<string> CallEnvironmentAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        var context = new AspectContext
        {
            AspectId = AspectId,
            SystemPrompt = SystemPrompt,
        };

        return await Environment.ProcessAsync(input, context, ct);
    }

    /// <summary>
    /// Determines whether this aspect should process a given thought.
    /// Default: process all thoughts. Override for selective processing.
    /// </summary>
    protected virtual bool ShouldProcess(string payload) => true;

    public async IAsyncEnumerable<Thought<string>> Process(
        IAsyncEnumerable<Thought<string>> input,
        GridCoordinate position,
        [EnumeratorCancellation] CancellationToken ct)
    {
        Activation = 1.0;

        await foreach (var thought in input.WithCancellation(ct))
        {
            if (!ShouldProcess(thought.Payload))
            {
                yield return thought; // Pass through unmodified
                continue;
            }

            var output = await TransformAsync(thought.Payload, position, ct);
            ProcessedCount++;

            yield return new Thought<string>
            {
                Payload = output,
                Origin = position,
                Timestamp = DateTimeOffset.UtcNow,
                TraceId = thought.TraceId,
                Metadata = new Dictionary<string, object>
                {
                    ["aspect"] = AspectId,
                    ["aspect_name"] = Name,
                    ["environment"] = Environment.Name,
                    ["source_origin"] = thought.Origin.ToString()
                }
            };
        }

        Activation = 0.0;
    }

    /// <summary>
    /// Directly query this aspect with a single thought (for interactive sub-entity access).
    /// </summary>
    public async Task<Thought<string>> QueryAsync(string input, GridCoordinate position, CancellationToken ct = default)
    {
        var output = await TransformAsync(input, position, ct);
        ProcessedCount++;

        return new Thought<string>
        {
            Payload = output,
            Origin = position,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["aspect"] = AspectId,
                ["environment"] = Environment.Name,
                ["query"] = true
            }
        };
    }

    /// <summary>
    /// Synchronous query shorthand — uses local transform only.
    /// </summary>
    public Thought<string> Query(string input, GridCoordinate position)
    {
        var output = TransformLocal(input, position);
        ProcessedCount++;

        return new Thought<string>
        {
            Payload = output,
            Origin = position,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["aspect"] = AspectId,
                ["query"] = true
            }
        };
    }
}
