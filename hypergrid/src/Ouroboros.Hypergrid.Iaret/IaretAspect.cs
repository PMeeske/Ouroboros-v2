namespace Ouroboros.Hypergrid.Iaret;

using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;

/// <summary>
/// A single interactable sub-entity of the convergent Iaret identity.
/// Each aspect occupies a position in the Hypergrid and processes thought
/// streams through its specialized lens (analytical, creative, guardian, etc.).
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

    protected IaretAspect(string aspectId, string name, int primaryDimension)
    {
        AspectId = aspectId ?? throw new ArgumentNullException(nameof(aspectId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PrimaryDimension = primaryDimension;
    }

    /// <summary>
    /// The core reasoning operation of this aspect. Transforms an input thought payload
    /// into an output payload through this aspect's specialized processing.
    /// </summary>
    protected abstract string Transform(string input, GridCoordinate position);

    /// <summary>
    /// Determines whether this aspect should process a given thought.
    /// Default: process all thoughts. Override for selective processing.
    /// </summary>
    protected virtual bool ShouldProcess(string payload) => true;

    public async IAsyncEnumerable<Thought<string>> Process(
        IAsyncEnumerable<Thought<string>> input,
        GridCoordinate position,
        CancellationToken ct)
    {
        Activation = 1.0;

        await foreach (var thought in input.WithCancellation(ct))
        {
            if (!ShouldProcess(thought.Payload))
            {
                yield return thought; // Pass through unmodified
                continue;
            }

            var output = Transform(thought.Payload, position);
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
                    ["source_origin"] = thought.Origin.ToString()
                }
            };
        }

        Activation = 0.0;
    }

    /// <summary>
    /// Directly query this aspect with a single thought (for interactive sub-entity access).
    /// </summary>
    public Thought<string> Query(string input, GridCoordinate position)
    {
        var output = Transform(input, position);
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
