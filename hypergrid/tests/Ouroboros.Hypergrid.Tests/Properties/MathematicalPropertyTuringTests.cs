namespace Ouroboros.Hypergrid.Tests.Properties;

using FluentAssertions;
using Ouroboros.Hypergrid.Routing;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Mathematical property Turing tests — validates the algebraic laws and invariants
/// that the Ouroboros ecosystem claims to uphold. These tests verify:
///
/// 1. Functor laws for Thought.Map
/// 2. Stream operator conservation laws
/// 3. Grid metric axioms
/// 4. Projection idempotency and composition
/// 5. Routing determinism
///
/// Inspired by the ML Whitepaper's formal properties (Section 14) and the
/// category-theoretic foundations of the Ouroboros architecture.
/// </summary>
public sealed class MathematicalPropertyTuringTests
{
    private static readonly GridCoordinate Origin = new(0, 0, 0);

    private static Thought<T> T<T>(T payload) => new()
    {
        Payload = payload,
        Origin = Origin,
        Timestamp = DateTimeOffset.UtcNow
    };

    // ══════════════════════════════════════════════════════════════════════
    // 1. FUNCTOR LAWS FOR Thought<T>.Map
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-999)]
    [InlineData(int.MaxValue)]
    public void Functor_identity_law_for_all_payloads(int value)
    {
        // Law: map(id) = id
        var thought = T(value);
        thought.Map(x => x).Payload.Should().Be(thought.Payload);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Functor_composition_law_for_arithmetic(int value)
    {
        // Law: map(f . g) = map(f) . map(g)
        Func<int, int> f = x => x * 3;
        Func<int, int> g = x => x + 7;

        var thought = T(value);

        var left = thought.Map(x => f(g(x)));
        var right = thought.Map(g).Map(f);

        left.Payload.Should().Be(right.Payload);
    }

    [Fact]
    public void Functor_composition_law_with_type_changes()
    {
        // map(f . g) = map(f) . map(g) across type boundaries
        var thought = T(42);
        Func<int, string> g = x => x.ToString();
        Func<string, int> f = s => s.Length;

        var composed = thought.Map(x => f(g(x)));
        var sequential = thought.Map(g).Map(f);

        composed.Payload.Should().Be(sequential.Payload);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. METRIC AXIOMS FOR GridCoordinate.ManhattanDistance
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(new[] { 0, 0 })]
    [InlineData(new[] { 3, 7, 1 })]
    [InlineData(new[] { -5, 10, 0, 3 })]
    public void Metric_identity_of_indiscernibles(int[] components)
    {
        // d(x, x) = 0
        var x = new GridCoordinate(components);
        x.ManhattanDistance(x).Should().Be(0);
    }

    [Fact]
    public void Metric_symmetry_property()
    {
        // d(x, y) = d(y, x) for all x, y
        var pairs = new[]
        {
            (new GridCoordinate(0, 0), new GridCoordinate(3, 4)),
            (new GridCoordinate(1, 2, 3), new GridCoordinate(4, 5, 6)),
            (new GridCoordinate(-1, -2), new GridCoordinate(1, 2)),
        };

        foreach (var (x, y) in pairs)
        {
            x.ManhattanDistance(y).Should().Be(y.ManhattanDistance(x),
                $"d({x}, {y}) should equal d({y}, {x})");
        }
    }

    [Fact]
    public void Metric_triangle_inequality_property()
    {
        // d(x, z) <= d(x, y) + d(y, z) for all x, y, z
        var x = new GridCoordinate(0, 0, 0);
        var y = new GridCoordinate(1, 2, 0);
        var z = new GridCoordinate(3, 1, 2);

        var dxz = x.ManhattanDistance(z);
        var dxy = x.ManhattanDistance(y);
        var dyz = y.ManhattanDistance(z);

        dxz.Should().BeLessThanOrEqualTo(dxy + dyz);
    }

    [Fact]
    public void Metric_non_negativity()
    {
        // d(x, y) >= 0 for all x, y
        var x = new GridCoordinate(-10, 20, -30);
        var y = new GridCoordinate(10, -20, 30);

        x.ManhattanDistance(y).Should().BeGreaterThanOrEqualTo(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. PROJECTION PROPERTIES
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Projection_idempotency(int dim)
    {
        // Project(Project(c, d, v), d, v) = Project(c, d, v)
        var coord = new GridCoordinate(1, 2, 3);
        var once = DimensionalProjection.Project(coord, dim, 99);
        var twice = DimensionalProjection.Project(once, dim, 99);

        twice.Should().Be(once);
    }

    [Fact]
    public void Projection_commutativity_on_different_dimensions()
    {
        // Project(Project(c, d1, v1), d2, v2) = Project(Project(c, d2, v2), d1, v1)
        // when d1 != d2
        var coord = new GridCoordinate(1, 2, 3);

        var leftFirst = DimensionalProjection.Project(
            DimensionalProjection.Project(coord, 0, 10), 2, 30);
        var rightFirst = DimensionalProjection.Project(
            DimensionalProjection.Project(coord, 2, 30), 0, 10);

        leftFirst.Should().Be(rightFirst);
    }

    [Fact]
    public void Projection_preserves_rank()
    {
        var coord = new GridCoordinate(1, 2, 3, 4, 5);
        var projected = DimensionalProjection.Project(coord, 3, 0);

        projected.Rank.Should().Be(coord.Rank);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. STREAM CONSERVATION LAWS
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public async Task Stream_where_true_preserves_count(int count)
    {
        // |where(true, s)| = |s|
        var thoughts = Enumerable.Range(0, count).Select(i => T(i)).ToList();
        var filtered = 0;

        await foreach (var _ in ThoughtStream.From(thoughts).Where(_ => true))
            filtered++;

        filtered.Should().Be(count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Stream_where_false_produces_zero(int count)
    {
        // |where(false, s)| = 0
        var thoughts = Enumerable.Range(0, count).Select(i => T(i)).ToList();
        var filtered = 0;

        await foreach (var _ in ThoughtStream.From(thoughts).Where(_ => false))
            filtered++;

        filtered.Should().Be(0);
    }

    [Fact]
    public async Task Stream_select_preserves_count()
    {
        // |map(f, s)| = |s| for all f
        var thoughts = Enumerable.Range(0, 7).Select(i => T(i)).ToList();
        var count = 0;

        await foreach (var _ in ThoughtStream.From(thoughts).Select(x => x * 100))
            count++;

        count.Should().Be(7);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. SPLIT CONSERVATION LAW
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    public async Task Split_conserves_total_element_count(int n)
    {
        // |matching| + |nonMatching| = |original| for all predicates
        var thoughts = Enumerable.Range(0, n).Select(i => T(i)).ToList();
        var (matching, nonMatching) = StreamOperators.Split(
            ThoughtStream.From(thoughts), x => x % 3 == 0);

        var matchCount = 0;
        await foreach (var _ in matching.ReadAllAsync()) matchCount++;

        var nonMatchCount = 0;
        await foreach (var _ in nonMatching.ReadAllAsync()) nonMatchCount++;

        (matchCount + nonMatchCount).Should().Be(n);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 6. ROUTING DETERMINISM
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Routing_is_deterministic_for_same_topology_and_policy()
    {
        var dims = new[]
        {
            new DimensionDescriptor(0, "temporal", ""),
            new DimensionDescriptor(1, "semantic", "")
        };
        var space = new HypergridSpace(dims);
        space.AddCell(new GridCoordinate(0, 0), "A");
        space.AddCell(new GridCoordinate(1, 0), "B");
        space.AddCell(new GridCoordinate(0, 1), "C");
        space.Connect(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0);
        space.Connect(new GridCoordinate(0, 0), new GridCoordinate(0, 1), 1);

        var router = new StreamRouter(space, FlowPolicy.Broadcast);

        // Run 100 times — must always produce the same result
        var baseline = router.ResolveTargets(new GridCoordinate(0, 0));
        for (var i = 0; i < 100; i++)
        {
            var result = router.ResolveTargets(new GridCoordinate(0, 0));
            result.Should().BeEquivalentTo(baseline);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 7. SLICE CONSISTENCY
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Slice_length_equals_range_size()
    {
        var origin = new GridCoordinate(0, 0, 0);
        var from = 3;
        var to = 10;

        var slice = DimensionalProjection.Slice(origin, 0, from, to).ToList();
        slice.Should().HaveCount(to - from + 1);
    }

    [Fact]
    public void Slice_all_elements_share_non_sliced_dimensions()
    {
        var origin = new GridCoordinate(5, 10, 15);
        var slice = DimensionalProjection.Slice(origin, 1, 0, 20).ToList();

        slice.Should().AllSatisfy(c =>
        {
            c[0].Should().Be(5);
            c[2].Should().Be(15);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // 8. EDGE WEIGHT ORDERING
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Nearest_routing_should_respect_weight_ordering()
    {
        var dims = new[] { new DimensionDescriptor(0, "d", "") };
        var space = new HypergridSpace(dims);
        var src = new GridCoordinate(0);

        // Note: we can't set custom weights through HypergridSpace.Connect directly
        // (it returns a GridEdge with default weight 1.0), but we can verify
        // that Nearest picks exactly one target from multiple equal-weight edges
        space.AddCell(src, "S");
        space.AddCell(new GridCoordinate(1), "A");
        space.AddCell(new GridCoordinate(2), "B");
        space.Connect(src, new GridCoordinate(1), 0);
        space.Connect(src, new GridCoordinate(2), 0);

        var router = new StreamRouter(space, FlowPolicy.Nearest);
        var targets = router.ResolveTargets(src);

        targets.Should().ContainSingle("Nearest should select exactly one");
    }
}
