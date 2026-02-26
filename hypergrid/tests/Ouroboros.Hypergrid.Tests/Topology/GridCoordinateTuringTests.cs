namespace Ouroboros.Hypergrid.Tests.Topology;

using FluentAssertions;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for GridCoordinate — validates that the N-dimensional coordinate
/// system behaves with mathematical correctness across arbitrary dimensionalities.
/// A coordinate system that "thinks" in N dimensions must respect metric axioms,
/// exhibit structural equality, and handle edge cases gracefully.
/// </summary>
public sealed class GridCoordinateTuringTests
{
    // ── Construction & Rank ─────────────────────────────────────────────

    [Theory]
    [InlineData(new[] { 0 }, 1)]
    [InlineData(new[] { 1, 2 }, 2)]
    [InlineData(new[] { 0, 0, 0 }, 3)]
    [InlineData(new[] { 3, 7, 1, 4, 1, 5 }, 6)]
    public void Rank_should_match_component_count(int[] components, int expectedRank)
    {
        var coord = new GridCoordinate(components);
        coord.Rank.Should().Be(expectedRank);
    }

    [Fact]
    public void Construction_with_empty_components_should_throw()
    {
        var act = () => new GridCoordinate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Construction_with_null_should_throw()
    {
        var act = () => new GridCoordinate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Indexer Access ──────────────────────────────────────────────────

    [Fact]
    public void Indexer_should_retrieve_correct_dimensional_value()
    {
        var coord = new GridCoordinate(10, 20, 30);

        coord[0].Should().Be(10, "dim-0 (temporal)");
        coord[1].Should().Be(20, "dim-1 (semantic)");
        coord[2].Should().Be(30, "dim-2 (causal)");
    }

    [Fact]
    public void Indexer_out_of_range_should_throw()
    {
        var coord = new GridCoordinate(1, 2);
        var act = () => coord[5];
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Structural Equality (Record Semantics) ──────────────────────────

    [Fact]
    public void Identical_coordinates_should_be_structurally_equal()
    {
        var a = new GridCoordinate(1, 2, 3);
        var b = new GridCoordinate(1, 2, 3);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_coordinates_should_not_be_equal()
    {
        var a = new GridCoordinate(1, 2, 3);
        var b = new GridCoordinate(1, 2, 4);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Coordinates_of_different_rank_should_not_be_equal()
    {
        var twoD = new GridCoordinate(0, 0);
        var threeD = new GridCoordinate(0, 0, 0);

        twoD.Should().NotBe(threeD);
    }

    // ── Manhattan Distance (Metric Axioms) ──────────────────────────────

    [Fact]
    public void Manhattan_distance_identity_of_indiscernibles()
    {
        // d(x, x) = 0
        var x = new GridCoordinate(3, 7, 2);
        x.ManhattanDistance(x).Should().Be(0);
    }

    [Fact]
    public void Manhattan_distance_symmetry()
    {
        // d(x, y) = d(y, x)
        var x = new GridCoordinate(0, 0, 0);
        var y = new GridCoordinate(3, 4, 5);

        x.ManhattanDistance(y).Should().Be(y.ManhattanDistance(x));
    }

    [Fact]
    public void Manhattan_distance_triangle_inequality()
    {
        // d(x, z) <= d(x, y) + d(y, z)
        var x = new GridCoordinate(0, 0);
        var y = new GridCoordinate(3, 0);
        var z = new GridCoordinate(3, 4);

        var dxz = x.ManhattanDistance(z);
        var dxy = x.ManhattanDistance(y);
        var dyz = y.ManhattanDistance(z);

        dxz.Should().BeLessThanOrEqualTo(dxy + dyz);
    }

    [Theory]
    [InlineData(new[] { 0, 0 }, new[] { 3, 4 }, 7)]
    [InlineData(new[] { 1, 1, 1 }, new[] { 4, 5, 6 }, 12)]
    [InlineData(new[] { -2, 3 }, new[] { 2, -3 }, 10)]
    public void Manhattan_distance_should_compute_correctly(int[] a, int[] b, int expected)
    {
        new GridCoordinate(a).ManhattanDistance(new GridCoordinate(b)).Should().Be(expected);
    }

    [Fact]
    public void Manhattan_distance_with_mismatched_rank_should_throw()
    {
        var twoD = new GridCoordinate(0, 0);
        var threeD = new GridCoordinate(0, 0, 0);

        var act = () => twoD.ManhattanDistance(threeD);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Manhattan_distance_with_null_should_throw()
    {
        var coord = new GridCoordinate(0, 0);
        var act = () => coord.ManhattanDistance(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── High-Dimensional Behavior ───────────────────────────────────────

    [Fact]
    public void Should_handle_high_dimensional_coordinates()
    {
        // Simulate a 10-dimensional thought space
        var components = Enumerable.Range(0, 10).ToArray();
        var coord = new GridCoordinate(components);

        coord.Rank.Should().Be(10);
        coord[9].Should().Be(9);
    }

    [Fact]
    public void Origin_coordinate_should_have_zero_distance_to_itself_in_any_dimension()
    {
        for (var dim = 1; dim <= 7; dim++)
        {
            var origin = new GridCoordinate(new int[dim]);
            origin.ManhattanDistance(origin).Should().Be(0, $"origin in {dim}D should have 0 self-distance");
        }
    }

    // ── ToString Representation ─────────────────────────────────────────

    [Fact]
    public void ToString_should_render_human_readable_format()
    {
        var coord = new GridCoordinate(1, 2, 3);
        coord.ToString().Should().Be("(1, 2, 3)");
    }

    [Fact]
    public void ToString_single_dimension_should_render_correctly()
    {
        var coord = new GridCoordinate(42);
        coord.ToString().Should().Be("(42)");
    }

    // ── Immutability ────────────────────────────────────────────────────

    [Fact]
    public void Components_should_be_immutable_snapshot()
    {
        var original = new[] { 1, 2, 3 };
        var coord = new GridCoordinate(original);

        // Mutating the original array should not affect the coordinate
        original[0] = 999;
        coord[0].Should().Be(1, "coordinate should be an immutable snapshot");
    }
}
