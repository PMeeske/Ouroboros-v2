namespace Ouroboros.Hypergrid.Tests.Streams;

using FluentAssertions;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for Confluence — validates the multi-stream convergence point
/// that enables distributed reasoning to reconverge. In a Hypergrid where thoughts
/// propagate along multiple dimensional axes simultaneously, confluence is the
/// mechanism for synthesizing parallel reasoning paths back into unified understanding.
/// </summary>
public sealed class ConfluenceTuringTests
{
    private static Thought<T> MakeThought<T>(T payload, int dim0 = 0) => new()
    {
        Payload = payload,
        Origin = new GridCoordinate(dim0, 0, 0),
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── Emit (Merge All Sources) ────────────────────────────────────────

    [Fact]
    public async Task Emit_should_merge_all_registered_sources()
    {
        var confluence = new Confluence<string>();
        confluence
            .Add(ThoughtStream.From([MakeThought("from-temporal")]))
            .Add(ThoughtStream.From([MakeThought("from-semantic")]))
            .Add(ThoughtStream.From([MakeThought("from-causal")]));

        var collected = new List<string>();
        await foreach (var t in confluence.Emit())
            collected.Add(t.Payload);

        collected.Should().HaveCount(3);
        collected.Should().Contain(["from-temporal", "from-semantic", "from-causal"]);
    }

    [Fact]
    public async Task Emit_with_no_sources_should_produce_empty_stream()
    {
        var confluence = new Confluence<int>();

        var collected = new List<int>();
        await foreach (var t in confluence.Emit())
            collected.Add(t.Payload);

        collected.Should().BeEmpty();
    }

    [Fact]
    public async Task Emit_should_interleave_multi_element_streams()
    {
        var confluence = new Confluence<int>();
        confluence
            .Add(ThoughtStream.From([MakeThought(1), MakeThought(2), MakeThought(3)]))
            .Add(ThoughtStream.From([MakeThought(10), MakeThought(20)]));

        var collected = new List<int>();
        await foreach (var t in confluence.Emit())
            collected.Add(t.Payload);

        collected.Should().HaveCount(5);
        collected.Should().Contain([1, 2, 3, 10, 20]);
    }

    // ── CollectFirst (Barrier Synchronization) ──────────────────────────

    [Fact]
    public async Task CollectFirst_should_gather_first_thought_from_each_source()
    {
        var confluence = new Confluence<string>();
        confluence
            .Add(ThoughtStream.From([MakeThought("first-A"), MakeThought("second-A")]))
            .Add(ThoughtStream.From([MakeThought("first-B"), MakeThought("second-B")]))
            .Add(ThoughtStream.From([MakeThought("first-C")]));

        var batch = await confluence.CollectFirst();

        batch.Should().HaveCount(3);
        batch.Select(t => t.Payload).Should().Equal("first-A", "first-B", "first-C");
    }

    [Fact]
    public async Task CollectFirst_with_single_source_should_return_single_thought()
    {
        var confluence = new Confluence<int>();
        confluence.Add(ThoughtStream.From([MakeThought(42), MakeThought(99)]));

        var batch = await confluence.CollectFirst();

        batch.Should().ContainSingle().Which.Payload.Should().Be(42);
    }

    [Fact]
    public async Task CollectFirst_with_no_sources_should_return_empty()
    {
        var confluence = new Confluence<string>();
        var batch = await confluence.CollectFirst();
        batch.Should().BeEmpty();
    }

    // ── Fluent API ──────────────────────────────────────────────────────

    [Fact]
    public void Add_should_return_same_confluence_for_chaining()
    {
        var confluence = new Confluence<int>();
        var returned = confluence.Add(ThoughtStream.From([MakeThought(1)]));

        returned.Should().BeSameAs(confluence);
    }

    // ── Convergence Pattern: Multiple Dimensional Axes ──────────────────

    [Fact]
    public async Task Should_converge_thoughts_from_different_grid_regions()
    {
        // Simulate thoughts originating from different dimensional positions
        var temporalThought = new Thought<string>
        {
            Payload = "temporal-analysis",
            Origin = new GridCoordinate(5, 0, 0),
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = "trace-1"
        };

        var semanticThought = new Thought<string>
        {
            Payload = "semantic-analysis",
            Origin = new GridCoordinate(0, 3, 0),
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = "trace-1"
        };

        var causalThought = new Thought<string>
        {
            Payload = "causal-analysis",
            Origin = new GridCoordinate(0, 0, 7),
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = "trace-1"
        };

        var confluence = new Confluence<string>();
        confluence
            .Add(ThoughtStream.Of(temporalThought))
            .Add(ThoughtStream.Of(semanticThought))
            .Add(ThoughtStream.Of(causalThought));

        var batch = await confluence.CollectFirst();

        batch.Should().HaveCount(3);
        batch.Should().AllSatisfy(t => t.TraceId.Should().Be("trace-1"));

        // Verify each analysis came from a different dimensional region
        var origins = batch.Select(t => t.Origin).ToList();
        origins.Should().OnlyHaveUniqueItems();
    }

    // ── Large-Scale Convergence ─────────────────────────────────────────

    [Fact]
    public async Task Should_handle_convergence_of_many_streams()
    {
        var confluence = new Confluence<int>();

        for (var i = 0; i < 50; i++)
            confluence.Add(ThoughtStream.Of(MakeThought(i)));

        var batch = await confluence.CollectFirst();
        batch.Should().HaveCount(50);
        batch.Select(t => t.Payload).Should().BeEquivalentTo(Enumerable.Range(0, 50));
    }
}
