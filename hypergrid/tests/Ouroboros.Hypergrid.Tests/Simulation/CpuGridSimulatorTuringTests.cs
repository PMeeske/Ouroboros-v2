namespace Ouroboros.Hypergrid.Tests.Simulation;

using FluentAssertions;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for CpuGridSimulator — validates that the CPU-based thought
/// propagation simulation correctly computes activation updates, converges
/// to stable states, and handles edge cases. These tests actually RUN the
/// simulation — no mocks, no stubs, real computation.
/// </summary>
public sealed class CpuGridSimulatorTuringTests
{
    // ── Single Step ─────────────────────────────────────────────────────

    [Fact]
    public void Step_should_propagate_activation_through_single_edge()
    {
        // A --1.0--> B
        var state = new SimulationState(
            activations: [1.0, 0.0],
            edgeRowPtr: [0, 0, 1],      // B has 1 incoming edge
            edgeTargets: [0],            // from A
            edgeWeights: [1.0],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        next.Activations[0].Should().Be(1.0, "A has no incoming edges, retains value");
        next.Activations[1].Should().Be(1.0, "B receives A's activation * weight 1.0");
        next.StepNumber.Should().Be(1);
    }

    [Fact]
    public void Step_with_weighted_edges_should_scale_activation()
    {
        // A --0.5--> B
        var state = new SimulationState(
            activations: [2.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [0.5],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        next.Activations[1].Should().Be(1.0, "B = A(2.0) * weight(0.5)");
    }

    [Fact]
    public void Step_with_multiple_incoming_edges_should_sum()
    {
        // A --1.0--> C, B --1.0--> C
        var state = new SimulationState(
            activations: [3.0, 7.0, 0.0],
            edgeRowPtr: [0, 0, 0, 2],   // C has 2 incoming edges
            edgeTargets: [0, 1],         // from A and B
            edgeWeights: [1.0, 1.0],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        next.Activations[2].Should().Be(10.0, "C = A(3) + B(7)");
    }

    [Fact]
    public void Step_with_tanh_should_squash_to_bounded_range()
    {
        // A(100) --1.0--> B  =>  B = tanh(100) ≈ 1.0
        var state = new SimulationState(
            activations: [100.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Tanh);
        var next = sim.Step(state);

        next.Activations[1].Should().BeApproximately(1.0, 1e-10, "tanh(100) ≈ 1.0");
    }

    [Fact]
    public void Step_isolated_node_should_retain_activation()
    {
        var state = new SimulationState(
            activations: [42.0],
            edgeRowPtr: [0, 0],
            edgeTargets: [],
            edgeWeights: [],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        next.Activations[0].Should().Be(42.0);
    }

    // ── Convergence ─────────────────────────────────────────────────────

    [Fact]
    public void RunUntilConvergence_chain_with_damping_should_converge()
    {
        // A --0.9--> B --0.9--> C (linear chain with decay)
        // With SoftConvergence(0.9), activations should decay and converge
        var state = new SimulationState(
            activations: [1.0, 0.0, 0.0],
            edgeRowPtr: [0, 0, 1, 2],
            edgeTargets: [0, 1],
            edgeWeights: [0.9, 0.9],
            stepNumber: 0);

        using var sim = new CpuGridSimulator(ActivationFunctions.SoftConvergence(0.9));
        var (final, steps) = sim.RunUntilConvergence(state, convergenceThreshold: 1e-6, maxSteps: 1000);

        steps.Should().BeLessThan(1000, "should converge before max steps");
        final.StepNumber.Should().Be(steps);
    }

    [Fact]
    public void RunUntilConvergence_already_stable_should_take_one_step()
    {
        // All zeros, no edges — already converged
        var state = new SimulationState(
            activations: [0.0, 0.0],
            edgeRowPtr: [0, 0, 0],
            edgeTargets: [],
            edgeWeights: [],
            stepNumber: 0);

        using var sim = new CpuGridSimulator();
        var (_, steps) = sim.RunUntilConvergence(state);

        steps.Should().Be(1);
    }

    // ── GridStateBuilder Integration ────────────────────────────────────

    [Fact]
    public void Should_simulate_from_hypergrid_space()
    {
        var space = new HypergridSpace([
            new DimensionDescriptor(0, "temporal", ""),
            new DimensionDescriptor(1, "semantic", "")
        ]);

        space.AddCell(new GridCoordinate(0, 0), "A");
        space.AddCell(new GridCoordinate(1, 0), "B");
        space.AddCell(new GridCoordinate(0, 1), "C");
        space.Connect(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0);
        space.Connect(new GridCoordinate(0, 0), new GridCoordinate(0, 1), 1);

        var state = GridStateBuilder.Build(space, cell =>
            cell.NodeId == "A" ? 1.0 : 0.0);

        state.CellCount.Should().Be(3);
        state.EdgeCount.Should().Be(2);

        using var sim = new CpuGridSimulator(ActivationFunctions.Identity);
        var next = sim.Step(state);

        // B and C should receive activation from A
        // (exact indices depend on cell ordering, but total non-zero should be 3)
        next.Activations.Count(a => a > 0).Should().Be(3);
    }

    // ── Larger Grid Simulation ──────────────────────────────────────────

    [Fact]
    public void Should_simulate_4x4_grid_with_convergence()
    {
        // Build a 4x4 grid with edges flowing right and down
        var space = new HypergridSpace([
            new DimensionDescriptor(0, "x", ""),
            new DimensionDescriptor(1, "y", "")
        ]);

        for (var x = 0; x < 4; x++)
        for (var y = 0; y < 4; y++)
        {
            space.AddCell(new GridCoordinate(x, y), $"cell-{x}-{y}");
            if (x > 0) space.Connect(new GridCoordinate(x - 1, y), new GridCoordinate(x, y), 0);
            if (y > 0) space.Connect(new GridCoordinate(x, y - 1), new GridCoordinate(x, y), 1);
        }

        // Inject activation at top-left
        var state = GridStateBuilder.Build(space, cell =>
            cell.Position == new GridCoordinate(0, 0) ? 1.0 : 0.0);

        state.CellCount.Should().Be(16);

        using var sim = new CpuGridSimulator(ActivationFunctions.Tanh);
        var (final, steps) = sim.RunUntilConvergence(state, convergenceThreshold: 1e-8, maxSteps: 100);

        // After convergence, activation should have propagated across the grid
        final.Activations.Count(a => Math.Abs(a) > 1e-10).Should().BeGreaterThan(1,
            "activation should propagate beyond the source cell");

        steps.Should().BeGreaterThan(1, "propagation requires multiple steps");
    }

    // ── Backend Name ────────────────────────────────────────────────────

    [Fact]
    public void BackendName_should_be_CPU()
    {
        using var sim = new CpuGridSimulator();
        sim.BackendName.Should().Be("CPU");
    }
}
