using System.Runtime.CompilerServices;

namespace Ouroboros.Hypergrid.Streams;

/// <summary>
/// A multi-stream convergence point that collects thoughts from several
/// streams and emits aggregated results once all sources have produced at least one value.
/// </summary>
public sealed class Confluence<T>
{
    private readonly List<IAsyncEnumerable<Thought<T>>> _sources = new();

    public Confluence<T> Add(IAsyncEnumerable<Thought<T>> source)
    {
        _sources.Add(source);
        return this;
    }

    /// <summary>
    /// Merges all registered sources and emits thoughts as they arrive.
    /// </summary>
    public IAsyncEnumerable<Thought<T>> Emit(CancellationToken ct = default) =>
        StreamOperators.Merge<T>(_sources, ct);

    /// <summary>
    /// Collects the first thought from each source and returns them as a batch.
    /// </summary>
    public async Task<IReadOnlyList<Thought<T>>> CollectFirst(CancellationToken ct = default)
    {
        var results = new List<Thought<T>>();
        foreach (var source in _sources)
        {
            await foreach (var thought in source.WithCancellation(ct))
            {
                results.Add(thought);
                break;
            }
        }
        return results;
    }
}
