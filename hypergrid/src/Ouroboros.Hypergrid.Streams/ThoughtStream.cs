using System.Runtime.CompilerServices;

namespace Ouroboros.Hypergrid.Streams;

/// <summary>
/// Factory and extension methods for creating and composing thought streams.
/// A thought stream is <c>IAsyncEnumerable&lt;Thought&lt;T&gt;&gt;</c>.
/// </summary>
public static class ThoughtStream
{
    /// <summary>Wraps a single thought as a stream.</summary>
    public static async IAsyncEnumerable<Thought<T>> Of<T>(
        Thought<T> thought,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
        yield return thought;
    }

    /// <summary>Wraps multiple thoughts as a stream.</summary>
    public static async IAsyncEnumerable<Thought<T>> From<T>(
        IEnumerable<Thought<T>> thoughts,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        foreach (var thought in thoughts)
        {
            ct.ThrowIfCancellationRequested();
            yield return thought;
        }
    }

    /// <summary>Maps each thought's payload in a stream.</summary>
    public static async IAsyncEnumerable<Thought<TOut>> Select<TIn, TOut>(
        this IAsyncEnumerable<Thought<TIn>> source,
        Func<TIn, TOut> selector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var thought in source.WithCancellation(ct))
            yield return thought.Map(selector);
    }

    /// <summary>Filters a thought stream by predicate on the payload.</summary>
    public static async IAsyncEnumerable<Thought<T>> Where<T>(
        this IAsyncEnumerable<Thought<T>> source,
        Func<T, bool> predicate,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var thought in source.WithCancellation(ct))
        {
            if (predicate(thought.Payload))
                yield return thought;
        }
    }
}
