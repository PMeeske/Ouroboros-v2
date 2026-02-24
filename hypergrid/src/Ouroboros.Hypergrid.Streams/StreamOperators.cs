using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Ouroboros.Hypergrid.Streams;

/// <summary>
/// Operators for merging, splitting, and transforming thought streams.
/// </summary>
public static class StreamOperators
{
    /// <summary>
    /// Merges multiple thought streams into a single interleaved stream.
    /// Elements arrive in the order they are produced.
    /// </summary>
    public static async IAsyncEnumerable<Thought<T>> Merge<T>(
        IEnumerable<IAsyncEnumerable<Thought<T>>> sources,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<Thought<T>>();
        var tasks = sources.Select(async source =>
        {
            await foreach (var thought in source.WithCancellation(ct))
                await channel.Writer.WriteAsync(thought, ct);
        }).ToList();

        _ = Task.WhenAll(tasks).ContinueWith(_ => channel.Writer.Complete(), ct);

        await foreach (var thought in channel.Reader.ReadAllAsync(ct))
            yield return thought;
    }

    /// <summary>
    /// Splits a stream into two based on a predicate.
    /// Returns (matching, nonMatching) channel readers.
    /// </summary>
    public static (ChannelReader<Thought<T>> Matching, ChannelReader<Thought<T>> NonMatching) Split<T>(
        IAsyncEnumerable<Thought<T>> source,
        Func<T, bool> predicate,
        CancellationToken ct = default)
    {
        var matching = Channel.CreateUnbounded<Thought<T>>();
        var nonMatching = Channel.CreateUnbounded<Thought<T>>();

        _ = Task.Run(async () =>
        {
            await foreach (var thought in source.WithCancellation(ct))
            {
                if (predicate(thought.Payload))
                    await matching.Writer.WriteAsync(thought, ct);
                else
                    await nonMatching.Writer.WriteAsync(thought, ct);
            }
            matching.Writer.Complete();
            nonMatching.Writer.Complete();
        }, ct);

        return (matching.Reader, nonMatching.Reader);
    }
}
