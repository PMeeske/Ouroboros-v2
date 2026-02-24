namespace Ouroboros.Hypergrid.Streams;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// A processing cell that transforms a thought stream at a grid vertex.
/// </summary>
public interface IGridCell<TIn, TOut>
{
    /// <summary>
    /// Processes an incoming thought stream at the given grid position,
    /// producing an outgoing thought stream.
    /// </summary>
    IAsyncEnumerable<Thought<TOut>> Process(
        IAsyncEnumerable<Thought<TIn>> input,
        GridCoordinate position,
        CancellationToken ct);
}
