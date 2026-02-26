namespace Ouroboros.Hypergrid.Tests.Streams;

using FluentAssertions;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for ThoughtStream — validates that thought stream factories and
/// combinators correctly create, transform, and filter async sequences of thoughts.
/// These tests verify the streaming algebra that underpins Hypergrid's reasoning model.
/// </summary>
public sealed class ThoughtStreamTuringTests
{
    private static readonly GridCoordinate Origin = new(0, 0, 0);

    private static Thought<T> MakeThought<T>(T payload) => new()
    {
        Payload = payload,
        Origin = Origin,
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── Of (Single-Element Stream) ──────────────────────────────────────

    [Fact]
    public async Task Of_should_produce_exactly_one_thought()
    {
        var thought = MakeThought("singleton");
        var collected = new List<Thought<string>>();

        await foreach (var t in ThoughtStream.Of(thought))
            collected.Add(t);

        collected.Should().ContainSingle().Which.Payload.Should().Be("singleton");
    }

    // ── From (Multi-Element Stream) ─────────────────────────────────────

    [Fact]
    public async Task From_should_produce_all_thoughts_in_order()
    {
        var thoughts = Enumerable.Range(1, 5).Select(i => MakeThought(i)).ToList();
        var collected = new List<int>();

        await foreach (var t in ThoughtStream.From(thoughts))
            collected.Add(t.Payload);

        collected.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task From_empty_collection_should_produce_empty_stream()
    {
        var collected = new List<Thought<string>>();
        await foreach (var t in ThoughtStream.From(Array.Empty<Thought<string>>()))
            collected.Add(t);

        collected.Should().BeEmpty();
    }

    // ── Select (Functor Map on Stream) ──────────────────────────────────

    [Fact]
    public async Task Select_should_transform_all_payloads()
    {
        var thoughts = Enumerable.Range(1, 3).Select(i => MakeThought(i)).ToList();
        var stream = ThoughtStream.From(thoughts);

        var doubled = new List<int>();
        await foreach (var t in stream.Select(x => x * 2))
            doubled.Add(t.Payload);

        doubled.Should().Equal(2, 4, 6);
    }

    [Fact]
    public async Task Select_should_preserve_thought_metadata()
    {
        var original = new Thought<int>
        {
            Payload = 42,
            Origin = new GridCoordinate(7, 8, 9),
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = "trace-select"
        };

        await foreach (var t in ThoughtStream.Of(original).Select(x => x.ToString()))
        {
            t.Payload.Should().Be("42");
            t.Origin.Should().Be(new GridCoordinate(7, 8, 9));
            t.TraceId.Should().Be("trace-select");
        }
    }

    [Fact]
    public async Task Select_identity_should_preserve_stream()
    {
        // Functor law: map(id) == id
        var thoughts = Enumerable.Range(0, 4).Select(i => MakeThought(i)).ToList();
        var collected = new List<int>();

        await foreach (var t in ThoughtStream.From(thoughts).Select(x => x))
            collected.Add(t.Payload);

        collected.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public async Task Select_composition_should_equal_chained_selects()
    {
        // Functor law: map(f . g) == map(f) . map(g)
        var thoughts = new[] { MakeThought(3), MakeThought(7) };

        Func<int, int> f = x => x * 10;
        Func<int, int> g = x => x + 1;

        var composed = new List<int>();
        await foreach (var t in ThoughtStream.From(thoughts).Select(x => f(g(x))))
            composed.Add(t.Payload);

        var chained = new List<int>();
        await foreach (var t in ThoughtStream.From(thoughts).Select(g).Select(f))
            chained.Add(t.Payload);

        composed.Should().Equal(chained);
    }

    // ── Where (Filter) ──────────────────────────────────────────────────

    [Fact]
    public async Task Where_should_filter_by_payload_predicate()
    {
        var thoughts = Enumerable.Range(1, 10).Select(i => MakeThought(i)).ToList();
        var evens = new List<int>();

        await foreach (var t in ThoughtStream.From(thoughts).Where(x => x % 2 == 0))
            evens.Add(t.Payload);

        evens.Should().Equal(2, 4, 6, 8, 10);
    }

    [Fact]
    public async Task Where_false_predicate_should_produce_empty_stream()
    {
        var thoughts = Enumerable.Range(1, 5).Select(i => MakeThought(i)).ToList();
        var collected = new List<int>();

        await foreach (var t in ThoughtStream.From(thoughts).Where(_ => false))
            collected.Add(t.Payload);

        collected.Should().BeEmpty();
    }

    [Fact]
    public async Task Where_true_predicate_should_preserve_all_elements()
    {
        var thoughts = Enumerable.Range(1, 5).Select(i => MakeThought(i)).ToList();
        var collected = new List<int>();

        await foreach (var t in ThoughtStream.From(thoughts).Where(_ => true))
            collected.Add(t.Payload);

        collected.Should().Equal(1, 2, 3, 4, 5);
    }

    // ── Composition of Select + Where ───────────────────────────────────

    [Fact]
    public async Task Should_compose_select_and_where_for_pipeline_processing()
    {
        // Simulate: filter important thoughts, then extract summaries
        var thoughts = new[]
        {
            MakeThought("important: discovery of monads"),
            MakeThought("noise: random chatter"),
            MakeThought("important: Kleisli arrow composition"),
            MakeThought("noise: irrelevant"),
        };

        var results = new List<string>();

        var stream = ThoughtStream.From(thoughts)
            .Where(s => s.StartsWith("important:"))
            .Select(s => s.Replace("important: ", "").ToUpperInvariant());

        await foreach (var t in stream)
            results.Add(t.Payload);

        results.Should().Equal("DISCOVERY OF MONADS", "KLEISLI ARROW COMPOSITION");
    }

    // ── Cancellation Support ────────────────────────────────────────────

    [Fact]
    public async Task Of_should_respect_cancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () =>
        {
            await foreach (var _ in ThoughtStream.Of(MakeThought("test"), cts.Token))
            {
                // Should not reach here
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task From_should_respect_cancellation()
    {
        using var cts = new CancellationTokenSource();
        var thoughts = Enumerable.Range(1, 100).Select(i => MakeThought(i)).ToList();
        var count = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var t in ThoughtStream.From(thoughts, cts.Token))
            {
                count++;
                if (count == 3)
                    await cts.CancelAsync();
            }
        });

        count.Should().BeLessThan(100, "stream should stop after cancellation");
    }
}
