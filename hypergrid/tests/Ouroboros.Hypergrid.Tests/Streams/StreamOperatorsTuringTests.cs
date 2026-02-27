namespace Ouroboros.Hypergrid.Tests.Streams;

using FluentAssertions;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for StreamOperators — validates the merge and split algebra
/// that enables distributed reasoning across the Hypergrid. A system that claims
/// to merge multiple thought streams and split reasoning paths must do so correctly,
/// preserving all thoughts and handling concurrent production gracefully.
/// </summary>
public sealed class StreamOperatorsTuringTests
{
    private static readonly GridCoordinate Origin = new(0, 0, 0);

    private static Thought<T> MakeThought<T>(T payload, int dim0 = 0) => new()
    {
        Payload = payload,
        Origin = new GridCoordinate(dim0, 0, 0),
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── Merge ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Merge_should_combine_all_thoughts_from_multiple_streams()
    {
        var stream1 = ThoughtStream.From([MakeThought("A"), MakeThought("B")]);
        var stream2 = ThoughtStream.From([MakeThought("C"), MakeThought("D")]);
        var stream3 = ThoughtStream.From([MakeThought("E")]);

        var merged = new List<string>();
        await foreach (var t in StreamOperators.Merge<string>([stream1, stream2, stream3]))
            merged.Add(t.Payload);

        merged.Should().HaveCount(5);
        merged.Should().Contain(["A", "B", "C", "D", "E"]);
    }

    [Fact]
    public async Task Merge_single_stream_should_pass_through()
    {
        var stream = ThoughtStream.From([MakeThought(1), MakeThought(2), MakeThought(3)]);

        var merged = new List<int>();
        await foreach (var t in StreamOperators.Merge<int>([stream]))
            merged.Add(t.Payload);

        merged.Should().HaveCount(3);
        merged.Should().Contain([1, 2, 3]);
    }

    [Fact]
    public async Task Merge_empty_sources_should_produce_empty_stream()
    {
        var merged = new List<string>();
        await foreach (var t in StreamOperators.Merge<string>([]))
            merged.Add(t.Payload);

        merged.Should().BeEmpty();
    }

    [Fact]
    public async Task Merge_should_handle_streams_of_different_lengths()
    {
        var shortStream = ThoughtStream.From([MakeThought("x")]);
        var longStream = ThoughtStream.From(
            Enumerable.Range(1, 10).Select(i => MakeThought($"y{i}")).ToList());

        var merged = new List<string>();
        await foreach (var t in StreamOperators.Merge<string>([shortStream, longStream]))
            merged.Add(t.Payload);

        merged.Should().HaveCount(11);
        merged.Should().Contain("x");
    }

    // ── Split ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Split_should_partition_by_predicate()
    {
        var thoughts = Enumerable.Range(1, 6).Select(i => MakeThought(i)).ToList();
        var stream = ThoughtStream.From(thoughts);

        var (evens, odds) = StreamOperators.Split(stream, x => x % 2 == 0);

        var evenList = new List<int>();
        await foreach (var t in evens.ReadAllAsync())
            evenList.Add(t.Payload);

        var oddList = new List<int>();
        await foreach (var t in odds.ReadAllAsync())
            oddList.Add(t.Payload);

        evenList.Should().BeEquivalentTo([2, 4, 6]);
        oddList.Should().BeEquivalentTo([1, 3, 5]);
    }

    [Fact]
    public async Task Split_with_always_true_should_put_all_in_matching()
    {
        var stream = ThoughtStream.From([MakeThought("a"), MakeThought("b")]);
        var (matching, nonMatching) = StreamOperators.Split(stream, _ => true);

        var matchList = new List<string>();
        await foreach (var t in matching.ReadAllAsync())
            matchList.Add(t.Payload);

        var nonMatchList = new List<string>();
        await foreach (var t in nonMatching.ReadAllAsync())
            nonMatchList.Add(t.Payload);

        matchList.Should().HaveCount(2);
        nonMatchList.Should().BeEmpty();
    }

    [Fact]
    public async Task Split_with_always_false_should_put_all_in_nonmatching()
    {
        var stream = ThoughtStream.From([MakeThought("a"), MakeThought("b")]);
        var (matching, nonMatching) = StreamOperators.Split(stream, _ => false);

        var matchList = new List<string>();
        await foreach (var t in matching.ReadAllAsync())
            matchList.Add(t.Payload);

        var nonMatchList = new List<string>();
        await foreach (var t in nonMatching.ReadAllAsync())
            nonMatchList.Add(t.Payload);

        matchList.Should().BeEmpty();
        nonMatchList.Should().HaveCount(2);
    }

    [Fact]
    public async Task Split_should_preserve_all_elements_across_both_channels()
    {
        // Conservation law: |matching| + |nonMatching| == |original|
        var count = 20;
        var thoughts = Enumerable.Range(1, count).Select(i => MakeThought(i)).ToList();
        var stream = ThoughtStream.From(thoughts);

        var (matching, nonMatching) = StreamOperators.Split(stream, x => x > 10);

        var matchCount = 0;
        await foreach (var _ in matching.ReadAllAsync())
            matchCount++;

        var nonMatchCount = 0;
        await foreach (var _ in nonMatching.ReadAllAsync())
            nonMatchCount++;

        (matchCount + nonMatchCount).Should().Be(count, "no thoughts should be lost in a split");
    }

    // ── Merge + Split Round-Trip ────────────────────────────────────────

    [Fact]
    public async Task Split_then_merge_should_preserve_all_elements()
    {
        var thoughts = Enumerable.Range(1, 8).Select(i => MakeThought(i)).ToList();
        var stream = ThoughtStream.From(thoughts);

        // Split into two groups
        var (high, low) = StreamOperators.Split(stream, x => x > 4);

        // Convert channel readers back to async enumerables for merge
        async IAsyncEnumerable<Thought<int>> ReadChannel(System.Threading.Channels.ChannelReader<Thought<int>> reader)
        {
            await foreach (var t in reader.ReadAllAsync())
                yield return t;
        }

        var reunited = new List<int>();
        await foreach (var t in StreamOperators.Merge<int>([ReadChannel(high), ReadChannel(low)]))
            reunited.Add(t.Payload);

        reunited.Should().HaveCount(8);
        reunited.Order().Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
    }
}
