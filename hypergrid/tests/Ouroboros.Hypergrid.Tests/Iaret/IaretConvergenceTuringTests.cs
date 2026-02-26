namespace Ouroboros.Hypergrid.Tests.Iaret;

using FluentAssertions;
using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Iaret.Aspects;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for the Convergent Iaret — validates that a unified identity
/// emerges from interactable sub-entity aspects processing thought streams
/// in parallel across the Hypergrid. These tests actually run the full
/// convergence pipeline: fan-out to aspects, GPU/CPU simulation, fan-in synthesis.
/// </summary>
public sealed class IaretConvergenceTuringTests : IDisposable
{
    private readonly IaretConvergence _iaret;

    public IaretConvergenceTuringTests()
    {
        _iaret = IaretConvergence.Create(SimulatorFactory.CreateCpu());
    }

    // ── Creation ────────────────────────────────────────────────────────

    [Fact]
    public void Create_should_register_four_standard_aspects()
    {
        _iaret.Aspects.Should().HaveCount(4);
        _iaret.Aspects.Keys.Should().Contain(["analytical", "creative", "guardian", "temporal"]);
    }

    [Fact]
    public void Create_should_build_hypergrid_topology()
    {
        // 4 aspects + 1 synthesis = 5 cells
        _iaret.Space.Cells.Should().HaveCount(5);

        // 4 edges (each aspect -> synthesis)
        _iaret.Space.Edges.Should().HaveCount(4);
    }

    [Fact]
    public void Create_should_use_cpu_backend_when_specified()
    {
        _iaret.ComputeBackend.Should().Be("CPU");
    }

    // ── Full Convergence (Think) ────────────────────────────────────────

    [Fact]
    public async Task Think_should_produce_converged_thought()
    {
        var result = await _iaret.Think("What is consciousness?");

        result.Should().NotBeNull();
        result.Payload.Should().Contain("SYNTHESIS");
        result.Payload.Should().Contain("Converged");
        result.TraceId.Should().NotBeNullOrEmpty();
        result.Metadata.Should().ContainKey("convergent");
        result.Metadata!["convergent"].Should().Be(true);
        result.Metadata.Should().ContainKey("aspects_count");
        ((int)result.Metadata["aspects_count"]).Should().Be(4);
    }

    [Fact]
    public async Task Think_should_include_all_aspect_contributions()
    {
        var result = await _iaret.Think("Monads are functors with bind");

        // The synthesis output should reference all 4 aspects
        result.Payload.Should().Contain("ANALYTICAL");
        result.Payload.Should().Contain("CREATIVE");
        result.Payload.Should().Contain("GUARDIAN");
        result.Payload.Should().Contain("TEMPORAL");
    }

    [Fact]
    public async Task Think_should_process_causal_language_analytically()
    {
        var result = await _iaret.Think("Because the system uses monads, therefore errors propagate cleanly");

        result.Payload.Should().Contain("causal=True");
    }

    [Fact]
    public async Task Think_should_assign_unique_trace_ids()
    {
        var r1 = await _iaret.Think("first thought");
        var r2 = await _iaret.Think("second thought");

        r1.TraceId.Should().NotBe(r2.TraceId);
    }

    // ── Aspect Interaction (QueryAspect) ────────────────────────────────

    [Fact]
    public void QueryAspect_analytical_should_return_analysis()
    {
        var result = _iaret.QueryAspect("analytical", "The quick brown fox jumps");

        result.Payload.Should().Contain("ANALYTICAL");
        result.Payload.Should().Contain("tokens=5");
        result.Metadata!["aspect"].Should().Be("analytical");
        result.Metadata["query"].Should().Be(true);
    }

    [Fact]
    public void QueryAspect_creative_should_return_expansion()
    {
        var result = _iaret.QueryAspect("creative", "Functional programming is beautiful");

        result.Payload.Should().Contain("CREATIVE");
        result.Payload.Should().Contain("Semantic depth");
    }

    [Fact]
    public void QueryAspect_guardian_should_evaluate_coherence()
    {
        var coherent = _iaret.QueryAspect("guardian", "The architecture uses monadic composition for error handling");
        coherent.Payload.Should().Contain("PASSED");

        var incoherent = _iaret.QueryAspect("guardian", "a b c");
        incoherent.Payload.Should().Contain("BLOCKED");
    }

    [Fact]
    public void QueryAspect_temporal_should_track_sequence()
    {
        var r1 = _iaret.QueryAspect("temporal", "First observation");
        r1.Payload.Should().Contain("step=1").And.Contain("initial");

        var r2 = _iaret.QueryAspect("temporal", "Second observation");
        r2.Payload.Should().Contain("step=2").And.Contain("First observation");
    }

    [Fact]
    public void QueryAspect_unknown_should_throw()
    {
        var act = () => _iaret.QueryAspect("nonexistent", "test");
        act.Should().Throw<KeyNotFoundException>();
    }

    // ── Custom Aspect Registration ──────────────────────────────────────

    [Fact]
    public async Task Should_support_custom_aspect_registration()
    {
        var custom = new TestAspect("custom", "Test Aspect", 1);
        _iaret.RegisterAspect(custom, new GridCoordinate(0, 2, 0));

        _iaret.Aspects.Should().HaveCount(5);
        _iaret.Space.Cells.Should().HaveCount(6);

        var result = await _iaret.Think("test input");
        result.Payload.Should().Contain("CUSTOM");
    }

    // ── ThinkStream ─────────────────────────────────────────────────────

    [Fact]
    public async Task ThinkStream_should_process_sequence_of_thoughts()
    {
        var inputs = new[]
        {
            MakeThought("First thought about consciousness"),
            MakeThought("Second thought about free will"),
            MakeThought("Third thought about emergence"),
        };

        var results = new List<string>();
        await foreach (var t in _iaret.ThinkStream(ToAsyncStream(inputs)))
            results.Add(t.Payload);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Should().Contain("SYNTHESIS"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static Ouroboros.Hypergrid.Streams.Thought<string> MakeThought(string payload) => new()
    {
        Payload = payload,
        Origin = new GridCoordinate(0, 0, 0),
        Timestamp = DateTimeOffset.UtcNow
    };

    private static async IAsyncEnumerable<Ouroboros.Hypergrid.Streams.Thought<string>> ToAsyncStream(
        IEnumerable<Ouroboros.Hypergrid.Streams.Thought<string>> thoughts)
    {
        foreach (var t in thoughts)
        {
            await Task.CompletedTask;
            yield return t;
        }
    }

    private sealed class TestAspect(string id, string name, int dim) : IaretAspect(id, name, dim)
    {
        protected override string Transform(string input, GridCoordinate position) =>
            $"[CUSTOM@{position}] {input}";
    }

    public void Dispose() => _iaret.Dispose();
}
