namespace Ouroboros.Hypergrid.Tests.Routing;

using FluentAssertions;
using Ouroboros.Hypergrid.Routing;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for DimensionalProjection — validates the cross-dimensional
/// reasoning capability that allows thoughts to move between reasoning axes.
/// Projecting from temporal to causal, or slicing along a semantic axis,
/// must preserve coordinate integrity and handle boundary conditions.
/// </summary>
public sealed class DimensionalProjectionTuringTests
{
    // ── Project (Single-Dimension Replacement) ──────────────────────────

    [Fact]
    public void Project_should_replace_target_dimension_value()
    {
        var source = new GridCoordinate(1, 2, 3);
        var projected = DimensionalProjection.Project(source, targetDimension: 1, newValue: 99);

        projected.Should().Be(new GridCoordinate(1, 99, 3));
    }

    [Fact]
    public void Project_should_preserve_all_other_dimensions()
    {
        var source = new GridCoordinate(10, 20, 30, 40);
        var projected = DimensionalProjection.Project(source, targetDimension: 2, newValue: 0);

        projected[0].Should().Be(10);
        projected[1].Should().Be(20);
        projected[2].Should().Be(0);
        projected[3].Should().Be(40);
    }

    [Fact]
    public void Project_same_value_should_return_equivalent_coordinate()
    {
        var source = new GridCoordinate(5, 10, 15);
        var projected = DimensionalProjection.Project(source, 1, 10);

        projected.Should().Be(source);
    }

    [Fact]
    public void Project_should_not_mutate_source_coordinate()
    {
        var source = new GridCoordinate(1, 2, 3);
        _ = DimensionalProjection.Project(source, 0, 999);

        source[0].Should().Be(1, "source coordinate must be immutable");
    }

    [Fact]
    public void Project_negative_dimension_should_throw()
    {
        var source = new GridCoordinate(1, 2, 3);
        var act = () => DimensionalProjection.Project(source, -1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Project_dimension_beyond_rank_should_throw()
    {
        var source = new GridCoordinate(1, 2, 3);
        var act = () => DimensionalProjection.Project(source, 3, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Project_null_source_should_throw()
    {
        var act = () => DimensionalProjection.Project(null!, 0, 0);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Slice (Range Along a Dimension) ─────────────────────────────────

    [Fact]
    public void Slice_should_generate_coordinates_along_specified_dimension()
    {
        var origin = new GridCoordinate(0, 5, 0);
        var slice = DimensionalProjection.Slice(origin, dimension: 0, from: 0, to: 3).ToList();

        slice.Should().HaveCount(4);
        slice[0].Should().Be(new GridCoordinate(0, 5, 0));
        slice[1].Should().Be(new GridCoordinate(1, 5, 0));
        slice[2].Should().Be(new GridCoordinate(2, 5, 0));
        slice[3].Should().Be(new GridCoordinate(3, 5, 0));
    }

    [Fact]
    public void Slice_should_preserve_non_sliced_dimensions()
    {
        var origin = new GridCoordinate(7, 0, 3);
        var slice = DimensionalProjection.Slice(origin, dimension: 1, from: 0, to: 2).ToList();

        slice.Should().AllSatisfy(c =>
        {
            c[0].Should().Be(7);
            c[2].Should().Be(3);
        });
    }

    [Fact]
    public void Slice_single_point_should_produce_one_coordinate()
    {
        var origin = new GridCoordinate(1, 2, 3);
        var slice = DimensionalProjection.Slice(origin, dimension: 0, from: 5, to: 5).ToList();

        slice.Should().ContainSingle().Which.Should().Be(new GridCoordinate(5, 2, 3));
    }

    [Fact]
    public void Slice_empty_range_should_produce_empty_sequence()
    {
        var origin = new GridCoordinate(1, 2, 3);
        var slice = DimensionalProjection.Slice(origin, dimension: 0, from: 5, to: 3).ToList();

        slice.Should().BeEmpty();
    }

    // ── Cross-Dimensional Projection Scenarios ──────────────────────────

    [Fact]
    public void Should_project_temporal_thought_into_causal_axis()
    {
        // A thought at temporal position 5, semantic 0, causal 0
        // "What if this thought had causal depth 3?"
        var temporal = new GridCoordinate(5, 0, 0);
        var causal = DimensionalProjection.Project(temporal, targetDimension: 2, newValue: 3);

        causal.Should().Be(new GridCoordinate(5, 0, 3));
    }

    [Fact]
    public void Should_explore_semantic_neighborhood_via_slice()
    {
        // Starting from a fixed temporal/causal position, explore semantic neighbors
        var anchor = new GridCoordinate(2, 0, 1);
        var neighbors = DimensionalProjection.Slice(anchor, dimension: 1, from: -2, to: 2).ToList();

        neighbors.Should().HaveCount(5);
        neighbors.Select(c => c[1]).Should().Equal(-2, -1, 0, 1, 2);
        neighbors.Should().AllSatisfy(c =>
        {
            c[0].Should().Be(2);  // temporal preserved
            c[2].Should().Be(1);  // causal preserved
        });
    }

    [Fact]
    public void Double_projection_should_be_equivalent_to_multi_field_replacement()
    {
        var source = new GridCoordinate(1, 2, 3);

        // Project dim-0 to 10, then dim-2 to 30
        var projected = DimensionalProjection.Project(
            DimensionalProjection.Project(source, 0, 10),
            2, 30);

        projected.Should().Be(new GridCoordinate(10, 2, 30));
    }

    // ── High-Dimensional Projections ────────────────────────────────────

    [Fact]
    public void Should_work_in_5D_thought_space()
    {
        // 5D: temporal, semantic, causal, modal, extensible
        var coord = new GridCoordinate(0, 0, 0, 0, 0);
        var projected = DimensionalProjection.Project(coord, 4, 42);

        projected.Rank.Should().Be(5);
        projected[4].Should().Be(42);
    }

    [Fact]
    public void Slice_in_high_dimensions_should_work()
    {
        var coord = new GridCoordinate(1, 2, 3, 4, 5);
        var slice = DimensionalProjection.Slice(coord, dimension: 3, from: 0, to: 9).ToList();

        slice.Should().HaveCount(10);
        slice.Select(c => c[3]).Should().Equal(Enumerable.Range(0, 10));
    }
}
