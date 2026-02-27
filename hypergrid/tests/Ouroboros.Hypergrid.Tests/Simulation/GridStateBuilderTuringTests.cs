// <copyright file="GridStateBuilderTuringTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Ouroboros.Hypergrid.Tests.Simulation;

using FluentAssertions;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for GridStateBuilder -- validates the conversion from a
/// <see cref="HypergridSpace"/> topology into a flat <see cref="SimulationState"/>
/// with compressed sparse row (CSR) encoding of the incoming-edge adjacency matrix.
/// Correct CSR construction is critical for both CPU and GPU simulation kernels.
///
/// Note: GridCoordinate uses reference-based record equality for its IReadOnlyList
/// property, so tests must reuse the same coordinate instances across AddCell and
/// Connect calls to ensure dictionary lookups succeed in GridStateBuilder.Build.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GridStateBuilderTuringTests
{
    private static readonly DimensionDescriptor Dim0 = new(0, "temporal", "");
    private static readonly DimensionDescriptor Dim1 = new(1, "semantic", "");

    // -- Basic Construction --------------------------------------------------

    [Fact]
    public void Build_single_cell_no_edges_should_produce_valid_state()
    {
        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(new GridCoordinate(0, 0), "A");

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(1);
        state.EdgeCount.Should().Be(0);
        state.Activations.Should().AllBeEquivalentTo(0.0, "default activation is 0");
    }

    [Fact]
    public void Build_two_cells_one_edge_should_produce_correct_csr()
    {
        // A --> B  (reuse coordinate references for dictionary lookup compatibility)
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");
        space.AddCell(coordB, "B");
        space.Connect(coordA, coordB, 0);

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(2);
        state.EdgeCount.Should().Be(1);

        // CSR: incoming edges -- B has one incoming edge from A, A has none
        // The EdgeRowPtr length should be CellCount + 1
        state.EdgeRowPtr.Should().HaveCount(3);
    }

    [Fact]
    public void Build_with_custom_initial_activation_should_apply_function()
    {
        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(new GridCoordinate(0, 0), "hot");
        space.AddCell(new GridCoordinate(1, 0), "cold");

        var state = GridStateBuilder.Build(space, cell =>
            cell.NodeId == "hot" ? 1.0 : 0.0);

        state.Activations.Should().Contain(1.0);
        state.Activations.Should().Contain(0.0);
    }

    [Fact]
    public void Build_with_null_initial_activation_should_default_to_zero()
    {
        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(new GridCoordinate(0, 0), "A");
        space.AddCell(new GridCoordinate(1, 0), "B");

        var state = GridStateBuilder.Build(space, initialActivation: null);

        state.Activations.Should().AllBeEquivalentTo(0.0);
    }

    // -- Edge Encoding -------------------------------------------------------

    [Fact]
    public void Build_multiple_incoming_edges_should_be_encoded_in_csr()
    {
        // A --> C, B --> C
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);
        var coordC = new GridCoordinate(2, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");
        space.AddCell(coordB, "B");
        space.AddCell(coordC, "C");
        space.Connect(coordA, coordC, 0);
        space.Connect(coordB, coordC, 0);

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(3);
        state.EdgeCount.Should().Be(2);
    }

    [Fact]
    public void Build_chain_topology_should_encode_sequential_edges()
    {
        // A --> B --> C --> D
        var coords = new[]
        {
            new GridCoordinate(0, 0),
            new GridCoordinate(1, 0),
            new GridCoordinate(2, 0),
            new GridCoordinate(3, 0),
        };
        var names = new[] { "A", "B", "C", "D" };

        var space = new HypergridSpace([Dim0, Dim1]);
        for (var i = 0; i < coords.Length; i++)
            space.AddCell(coords[i], names[i]);

        space.Connect(coords[0], coords[1], 0);
        space.Connect(coords[1], coords[2], 0);
        space.Connect(coords[2], coords[3], 0);

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(4);
        state.EdgeCount.Should().Be(3);
        state.EdgeRowPtr.Should().HaveCount(5, "CellCount + 1 = 5");
    }

    [Fact]
    public void Build_edges_with_default_weight_should_preserve_weights()
    {
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");
        space.AddCell(coordB, "B");
        space.Connect(coordA, coordB, 0);

        var state = GridStateBuilder.Build(space);

        state.EdgeWeights.Should().HaveCount(1);
        state.EdgeWeights[0].Should().Be(1.0, "default edge weight");
    }

    // -- Empty Grid ----------------------------------------------------------

    [Fact]
    public void Build_empty_grid_should_produce_empty_state()
    {
        var space = new HypergridSpace([Dim0, Dim1]);

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(0);
        state.EdgeCount.Should().Be(0);
        state.EdgeRowPtr.Should().HaveCount(1, "empty grid still has EdgeRowPtr[0] = 0");
    }

    // -- Null Guard ----------------------------------------------------------

    [Fact]
    public void Build_null_space_should_throw()
    {
        var act = () => GridStateBuilder.Build(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // -- Integration with Simulation -----------------------------------------

    [Fact]
    public void Built_state_should_be_usable_by_cpu_simulator()
    {
        // A --> B with activation at A
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "source");
        space.AddCell(coordB, "sink");
        space.Connect(coordA, coordB, 0);

        var state = GridStateBuilder.Build(space, cell =>
            cell.NodeId == "source" ? 1.0 : 0.0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        // Both cells should have non-zero activation: source retains, sink receives
        next.Activations.Count(a => a > 0.0).Should().Be(2,
            "source retains activation and sink receives propagated activation");
    }

    [Fact]
    public void Built_state_from_2d_grid_should_match_cell_and_edge_counts()
    {
        // 3x3 grid with right and down edges -- pre-create coordinates for reference reuse
        var space = new HypergridSpace([Dim0, Dim1]);
        var coords = new GridCoordinate[3, 3];
        var expectedEdges = 0;

        for (var x = 0; x < 3; x++)
        for (var y = 0; y < 3; y++)
            coords[x, y] = new GridCoordinate(x, y);

        for (var x = 0; x < 3; x++)
        for (var y = 0; y < 3; y++)
        {
            space.AddCell(coords[x, y], $"cell-{x}-{y}");
            if (x > 0) { space.Connect(coords[x - 1, y], coords[x, y], 0); expectedEdges++; }
            if (y > 0) { space.Connect(coords[x, y - 1], coords[x, y], 1); expectedEdges++; }
        }

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(9);
        state.EdgeCount.Should().Be(expectedEdges);
        state.EdgeRowPtr.Should().HaveCount(10, "9 cells + 1");
    }

    // -- Edges Referencing Non-Existent Cells --------------------------------

    [Fact]
    public void Build_should_ignore_edges_referencing_nonexistent_cells()
    {
        var coordA = new GridCoordinate(0, 0);
        var coordGhost = new GridCoordinate(99, 99);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");

        // Connect to a coordinate that was never added as a cell
        space.Connect(coordA, coordGhost, 0);

        var state = GridStateBuilder.Build(space);

        state.CellCount.Should().Be(1);
        state.EdgeCount.Should().Be(0, "edge to nonexistent cell should be skipped");
    }

    // -- Step Number ---------------------------------------------------------

    [Fact]
    public void Build_should_produce_state_at_step_zero()
    {
        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(new GridCoordinate(0, 0), "A");

        var state = GridStateBuilder.Build(space);

        state.StepNumber.Should().Be(0);
    }

    // -- CSR Structure Correctness -------------------------------------------

    [Fact]
    public void Build_should_produce_correct_csr_for_fan_in_topology()
    {
        // A --> C, B --> C -- verify C has 2 incoming edges with correct source indices
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);
        var coordC = new GridCoordinate(2, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");
        space.AddCell(coordB, "B");
        space.AddCell(coordC, "C");
        space.Connect(coordA, coordC, 0);
        space.Connect(coordB, coordC, 0);

        var state = GridStateBuilder.Build(space, cell =>
            cell.NodeId switch { "A" => 3.0, "B" => 7.0, _ => 0.0 });

        // Verify via simulation: C should receive sum of A and B's activations
        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        // C = 3.0 + 7.0 = 10.0 (identity activation, both edges weight 1.0)
        next.Activations.Should().Contain(10.0,
            "C should receive sum of incoming activations from A and B");
    }

    [Fact]
    public void Build_should_produce_correct_activation_via_chain_propagation()
    {
        // A(5.0) --1.0--> B --1.0--> C
        // After one step: B = identity(5.0) = 5.0, C = identity(0.0) = 0.0 (B was 0)
        // After two steps: C = identity(5.0) = 5.0
        var coordA = new GridCoordinate(0, 0);
        var coordB = new GridCoordinate(1, 0);
        var coordC = new GridCoordinate(2, 0);

        var space = new HypergridSpace([Dim0, Dim1]);
        space.AddCell(coordA, "A");
        space.AddCell(coordB, "B");
        space.AddCell(coordC, "C");
        space.Connect(coordA, coordB, 0);
        space.Connect(coordB, coordC, 0);

        var state = GridStateBuilder.Build(space, cell =>
            cell.NodeId == "A" ? 5.0 : 0.0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var step1 = sim.Step(state);
        var step2 = sim.Step(step1);

        // After 2 steps, activation should have reached C
        step2.Activations.Should().Contain(5.0, "activation should propagate through chain");
    }
}
