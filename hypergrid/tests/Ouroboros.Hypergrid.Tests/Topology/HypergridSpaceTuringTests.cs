namespace Ouroboros.Hypergrid.Tests.Topology;

using FluentAssertions;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for HypergridSpace — validates that an N-dimensional thought space
/// correctly manages cells, edges, and dimensional structure. A space that claims to
/// host distributed reasoning must maintain topological consistency, handle dynamic
/// growth, and enforce dimensional constraints.
/// </summary>
public sealed class HypergridSpaceTuringTests
{
    private static readonly DimensionDescriptor Temporal = new(0, "temporal", "Time-ordered thought progression");
    private static readonly DimensionDescriptor Semantic = new(1, "semantic", "Conceptual similarity space");
    private static readonly DimensionDescriptor Causal = new(2, "causal", "Cause-effect reasoning chains");

    private static HypergridSpace CreateStandard3DSpace() =>
        new([Temporal, Semantic, Causal]);

    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Space_rank_should_match_dimension_count()
    {
        var space = CreateStandard3DSpace();
        space.Rank.Should().Be(3);
        space.Dimensions.Should().HaveCount(3);
    }

    [Fact]
    public void Space_with_no_dimensions_should_throw()
    {
        var act = () => new HypergridSpace([]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Space_with_null_dimensions_should_throw()
    {
        var act = () => new HypergridSpace(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Empty_space_should_have_no_cells_or_edges()
    {
        var space = CreateStandard3DSpace();
        space.Cells.Should().BeEmpty();
        space.Edges.Should().BeEmpty();
    }

    // ── Cell Management ─────────────────────────────────────────────────

    [Fact]
    public void AddCell_should_register_cell_at_coordinate()
    {
        var space = CreateStandard3DSpace();
        var pos = new GridCoordinate(0, 0, 0);

        var cell = space.AddCell(pos, "Ou-1");

        cell.Should().NotBeNull();
        cell.Position.Should().Be(pos);
        cell.NodeId.Should().Be("Ou-1");
        space.Cells.Should().HaveCount(1);
    }

    [Fact]
    public void AddCell_with_wrong_rank_should_throw()
    {
        var space = CreateStandard3DSpace();
        var pos = new GridCoordinate(0, 0); // 2D in a 3D space

        var act = () => space.AddCell(pos, "bad");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetCell_should_return_registered_cell()
    {
        var space = CreateStandard3DSpace();
        var pos = new GridCoordinate(1, 2, 3);
        space.AddCell(pos, "Ou-1");

        var retrieved = space.GetCell(pos);

        retrieved.Should().NotBeNull();
        retrieved!.NodeId.Should().Be("Ou-1");
    }

    [Fact]
    public void GetCell_for_unknown_position_should_return_null()
    {
        var space = CreateStandard3DSpace();
        space.GetCell(new GridCoordinate(9, 9, 9)).Should().BeNull();
    }

    [Fact]
    public void Adding_cell_at_same_position_should_replace_previous()
    {
        var space = CreateStandard3DSpace();
        var pos = new GridCoordinate(0, 0, 0);

        space.AddCell(pos, "Ou-1");
        space.AddCell(pos, "Ou-2");

        space.Cells.Should().HaveCount(1);
        space.GetCell(pos)!.NodeId.Should().Be("Ou-2");
    }

    // ── Edge Management ─────────────────────────────────────────────────

    [Fact]
    public void Connect_should_create_directed_edge()
    {
        var space = CreateStandard3DSpace();
        var src = new GridCoordinate(0, 0, 0);
        var tgt = new GridCoordinate(1, 0, 0);
        space.AddCell(src, "Ou-1");
        space.AddCell(tgt, "Ou-2");

        var edge = space.Connect(src, tgt, dimension: 0, label: "temporal-link");

        edge.Source.Should().Be(src);
        edge.Target.Should().Be(tgt);
        edge.Dimension.Should().Be(0);
        edge.Label.Should().Be("temporal-link");
        space.Edges.Should().HaveCount(1);
    }

    [Fact]
    public void GetEdgesFrom_should_return_only_outgoing_edges()
    {
        var space = CreateStandard3DSpace();
        var a = new GridCoordinate(0, 0, 0);
        var b = new GridCoordinate(1, 0, 0);
        var c = new GridCoordinate(0, 1, 0);

        space.Connect(a, b, 0);
        space.Connect(a, c, 1);
        space.Connect(b, c, 1);

        var fromA = space.GetEdgesFrom(a).ToList();
        fromA.Should().HaveCount(2);
        fromA.Should().AllSatisfy(e => e.Source.Should().Be(a));
    }

    [Fact]
    public void GetEdgesFrom_unknown_coordinate_should_return_empty()
    {
        var space = CreateStandard3DSpace();
        space.GetEdgesFrom(new GridCoordinate(99, 99, 99)).Should().BeEmpty();
    }

    // ── Multi-Dimensional Grid Construction ─────────────────────────────

    [Fact]
    public void Should_support_building_a_3x3_semantic_temporal_grid()
    {
        var space = CreateStandard3DSpace();

        // Build a 3x3 grid on the temporal-semantic plane (dim-0 x dim-1)
        for (var t = 0; t < 3; t++)
        for (var s = 0; s < 3; s++)
        {
            space.AddCell(new GridCoordinate(t, s, 0), $"Ou-{t}-{s}");
        }

        space.Cells.Should().HaveCount(9);

        // Wire temporal edges (along dim-0)
        for (var t = 0; t < 2; t++)
        for (var s = 0; s < 3; s++)
        {
            space.Connect(new GridCoordinate(t, s, 0), new GridCoordinate(t + 1, s, 0), 0);
        }

        // Wire semantic edges (along dim-1)
        for (var t = 0; t < 3; t++)
        for (var s = 0; s < 2; s++)
        {
            space.Connect(new GridCoordinate(t, s, 0), new GridCoordinate(t, s + 1, 0), 1);
        }

        space.Edges.Should().HaveCount(12); // 6 temporal + 6 semantic
    }

    [Fact]
    public void Should_support_single_dimension_grid()
    {
        var space = new HypergridSpace([new DimensionDescriptor(0, "temporal", "Time")]);

        space.AddCell(new GridCoordinate(0), "start");
        space.AddCell(new GridCoordinate(1), "end");
        space.Connect(new GridCoordinate(0), new GridCoordinate(1), 0);

        space.Rank.Should().Be(1);
        space.Cells.Should().HaveCount(2);
        space.Edges.Should().HaveCount(1);
    }

    // ── Dimension Descriptors ───────────────────────────────────────────

    [Fact]
    public void Dimension_descriptors_should_preserve_names()
    {
        var space = CreateStandard3DSpace();

        space.Dimensions[0].Name.Should().Be("temporal");
        space.Dimensions[1].Name.Should().Be("semantic");
        space.Dimensions[2].Name.Should().Be("causal");
    }

    // ── Cell State Lifecycle ────────────────────────────────────────────

    [Fact]
    public void New_cell_should_start_in_idle_state()
    {
        var cell = new GridCell(new GridCoordinate(0, 0, 0), "node");
        cell.State.Should().Be(GridCellState.Idle);
    }

    [Fact]
    public void Cell_should_transition_through_lifecycle_states()
    {
        var cell = new GridCell(new GridCoordinate(0, 0, 0), "node");

        cell.State.Should().Be(GridCellState.Idle);

        cell.Activate();
        cell.State.Should().Be(GridCellState.Active);

        cell.Fault("LLM timeout");
        cell.State.Should().Be(GridCellState.Faulted);

        cell.Deactivate();
        cell.State.Should().Be(GridCellState.Idle);
    }

    // ── Edge Weight ─────────────────────────────────────────────────────

    [Fact]
    public void Edge_default_weight_should_be_one()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0, 0), new GridCoordinate(1, 0, 0), 0);
        edge.Weight.Should().Be(1.0);
    }

    [Fact]
    public void Edge_weight_should_be_customizable()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0, 0), new GridCoordinate(1, 0, 0), 0) { Weight = 0.5 };
        edge.Weight.Should().Be(0.5);
    }
}
