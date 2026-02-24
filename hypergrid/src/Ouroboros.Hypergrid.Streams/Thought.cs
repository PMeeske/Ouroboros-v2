namespace Ouroboros.Hypergrid.Streams;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// A discrete unit of reasoning flowing through the Hypergrid.
/// Carries a typed payload with positional and temporal metadata.
/// </summary>
public sealed record Thought<T>
{
    public required T Payload { get; init; }
    public required GridCoordinate Origin { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public Thought<TOut> Map<TOut>(Func<T, TOut> f) => new()
    {
        Payload = f(Payload),
        Origin = Origin,
        Timestamp = Timestamp,
        TraceId = TraceId,
        Metadata = Metadata
    };
}
