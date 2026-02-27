namespace Ouroboros.Hypergrid.Tests.Iaret;

using FluentAssertions;
using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Iaret.Aspects;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for individual Iaret aspects — validates that each sub-entity
/// correctly implements its specialized reasoning when processing thought streams.
/// These tests verify that aspects are genuinely interactable sub-entities,
/// not just passive pipeline stages.
/// </summary>
public sealed class AspectTuringTests
{
    private static readonly GridCoordinate Pos = new(0, 0, 0);

    private static Thought<string> MakeThought(string payload) => new()
    {
        Payload = payload,
        Origin = Pos,
        Timestamp = DateTimeOffset.UtcNow
    };

    // ── Analytical Aspect ───────────────────────────────────────────────

    [Fact]
    public async Task Analytical_should_count_tokens()
    {
        var aspect = new AnalyticalAspect();
        var input = ThoughtStream.Of(MakeThought("the quick brown fox jumps"));

        var results = new List<string>();
        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
            results.Add(t.Payload);

        results.Should().ContainSingle().Which.Should().Contain("tokens=5");
    }

    [Fact]
    public async Task Analytical_should_detect_causal_markers()
    {
        var aspect = new AnalyticalAspect();

        var causal = ThoughtStream.Of(MakeThought("Because X, therefore Y"));
        await foreach (var t in aspect.Process(causal, Pos, CancellationToken.None))
            t.Payload.Should().Contain("causal=True");

        var nonCausal = ThoughtStream.Of(MakeThought("The sky is blue"));
        await foreach (var t in aspect.Process(nonCausal, Pos, CancellationToken.None))
            t.Payload.Should().Contain("causal=False");
    }

    [Fact]
    public async Task Analytical_should_detect_questions()
    {
        var aspect = new AnalyticalAspect();
        var question = ThoughtStream.Of(MakeThought("What is the meaning of life?"));

        await foreach (var t in aspect.Process(question, Pos, CancellationToken.None))
            t.Payload.Should().Contain("interrogative=True");
    }

    [Fact]
    public async Task Analytical_should_tag_with_aspect_metadata()
    {
        var aspect = new AnalyticalAspect();
        var input = ThoughtStream.Of(MakeThought("test"));

        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
        {
            t.Metadata.Should().ContainKey("aspect");
            t.Metadata!["aspect"].Should().Be("analytical");
        }
    }

    // ── Creative Aspect ─────────────────────────────────────────────────

    [Fact]
    public async Task Creative_should_extract_key_concepts()
    {
        var aspect = new CreativeAspect();
        var input = ThoughtStream.Of(MakeThought("Functional programming enables mathematical composition"));

        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
        {
            t.Payload.Should().Contain("CREATIVE");
            t.Payload.Should().Contain("Semantic depth");
        }
    }

    [Fact]
    public async Task Creative_should_vary_connectors_across_thoughts()
    {
        var aspect = new CreativeAspect();
        var thoughts = new[]
        {
            MakeThought("first complex thought here"),
            MakeThought("second complex thought here"),
        };
        var stream = ThoughtStream.From(thoughts);

        var results = new List<string>();
        await foreach (var t in aspect.Process(stream, Pos, CancellationToken.None))
            results.Add(t.Payload);

        results.Should().HaveCount(2);
        // Different connectors should be used
        results[0].Should().NotBe(results[1]);
    }

    // ── Guardian Aspect ─────────────────────────────────────────────────

    [Fact]
    public async Task Guardian_should_pass_coherent_thoughts()
    {
        var aspect = new GuardianAspect(coherenceThreshold: 0.3);
        var input = ThoughtStream.Of(MakeThought("The architecture uses monadic composition for safe error handling"));

        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
            t.Payload.Should().Contain("PASSED");
    }

    [Fact]
    public async Task Guardian_should_block_incoherent_thoughts()
    {
        var aspect = new GuardianAspect(coherenceThreshold: 0.8);
        var input = ThoughtStream.Of(MakeThought("a b c d"));

        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
            t.Payload.Should().Contain("BLOCKED");
    }

    [Fact]
    public async Task Guardian_should_pass_through_empty_thoughts_unprocessed()
    {
        var aspect = new GuardianAspect();
        var input = ThoughtStream.Of(MakeThought("   "));

        var count = 0;
        await foreach (var t in aspect.Process(input, Pos, CancellationToken.None))
        {
            count++;
            // Empty thought passed through unprocessed (ShouldProcess = false)
            t.Payload.Should().Be("   ");
        }
        count.Should().Be(1);
    }

    [Fact]
    public async Task Guardian_should_track_blocked_count()
    {
        var aspect = new GuardianAspect(coherenceThreshold: 0.99);
        var thoughts = new[]
        {
            MakeThought("short"),
            MakeThought("another short one"),
            MakeThought("a b c"),
        };
        var stream = ThoughtStream.From(thoughts);

        await foreach (var _ in aspect.Process(stream, Pos, CancellationToken.None)) { }

        aspect.BlockedCount.Should().BeGreaterThan(0);
    }

    // ── Temporal Aspect ─────────────────────────────────────────────────

    [Fact]
    public async Task Temporal_should_track_sequence_positions()
    {
        var aspect = new TemporalAspect(windowSize: 3);
        var thoughts = new[]
        {
            MakeThought("observation one"),
            MakeThought("observation two"),
            MakeThought("observation three"),
        };
        var stream = ThoughtStream.From(thoughts);

        var results = new List<string>();
        await foreach (var t in aspect.Process(stream, Pos, CancellationToken.None))
            results.Add(t.Payload);

        results[0].Should().Contain("step=1").And.Contain("initial");
        results[1].Should().Contain("step=2").And.Contain("observation one");
        results[2].Should().Contain("step=3").And.Contain("observation two");
    }

    [Fact]
    public async Task Temporal_should_maintain_sliding_window()
    {
        var aspect = new TemporalAspect(windowSize: 2);
        var thoughts = Enumerable.Range(1, 5).Select(i => MakeThought($"thought-{i}")).ToList();

        await foreach (var _ in aspect.Process(ThoughtStream.From(thoughts), Pos, CancellationToken.None)) { }

        aspect.Context.Should().HaveCount(2, "window size is 2");
        aspect.Context.Should().Contain("thought-4");
        aspect.Context.Should().Contain("thought-5");
    }

    // ── Synthesis Aspect ────────────────────────────────────────────────

    [Fact]
    public void Synthesis_should_merge_multiple_aspect_outputs()
    {
        var synthesis = new SynthesisAspect();
        var outputs = new[]
        {
            "[ANALYTICAL@(0, 0, 1)] tokens=5 | some analysis",
            "[CREATIVE@(0, 1, 0)] some creative expansion",
            "[GUARDIAN@(1, 0, 0)] PASSED | validated output",
        };

        var result = synthesis.Synthesize(outputs, Pos);

        result.Should().Contain("SYNTHESIS");
        result.Should().Contain("Converged 3 streams");
        result.Should().Contain("ANALYTICAL");
        result.Should().Contain("CREATIVE");
        result.Should().Contain("GUARDIAN");
    }

    [Fact]
    public void Synthesis_with_no_inputs_should_indicate_absence()
    {
        var synthesis = new SynthesisAspect();
        var result = synthesis.Synthesize([], Pos);
        result.Should().Contain("No aspects contributed");
    }

    // ── Aspect Properties ───────────────────────────────────────────────

    [Fact]
    public void All_aspects_should_have_correct_identifiers()
    {
        new AnalyticalAspect().AspectId.Should().Be("analytical");
        new CreativeAspect().AspectId.Should().Be("creative");
        new GuardianAspect().AspectId.Should().Be("guardian");
        new TemporalAspect().AspectId.Should().Be("temporal");
        new SynthesisAspect().AspectId.Should().Be("synthesis");
    }

    [Fact]
    public void All_aspects_should_have_names()
    {
        new AnalyticalAspect().Name.Should().Be("The Analytical Eye");
        new CreativeAspect().Name.Should().Be("The Creative Flame");
        new GuardianAspect().Name.Should().Be("The Guardian Uraeus");
        new TemporalAspect().Name.Should().Be("The Temporal Weaver");
        new SynthesisAspect().Name.Should().Be("The Synthesis Crown");
    }

    [Fact]
    public async Task ProcessedCount_should_increment_per_thought()
    {
        var aspect = new AnalyticalAspect();
        var thoughts = Enumerable.Range(0, 5).Select(i => MakeThought($"thought {i}")).ToList();

        await foreach (var _ in aspect.Process(ThoughtStream.From(thoughts), Pos, CancellationToken.None)) { }

        aspect.ProcessedCount.Should().Be(5);
    }

    // ── Direct Query (Interactable Sub-Entity) ──────────────────────────

    [Fact]
    public void Query_should_return_single_thought_without_stream()
    {
        var aspect = new AnalyticalAspect();
        var result = aspect.Query("test input", Pos);

        result.Should().NotBeNull();
        result.Payload.Should().Contain("ANALYTICAL");
        result.Metadata!["query"].Should().Be(true);
        aspect.ProcessedCount.Should().Be(1);
    }
}
