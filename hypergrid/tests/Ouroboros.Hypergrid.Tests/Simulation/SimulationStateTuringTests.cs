namespace Ouroboros.Hypergrid.Tests.Simulation;

using FluentAssertions;
using Ouroboros.Hypergrid.Simulation;
using Xunit;

/// <summary>
/// Turing tests for SimulationState — validates the immutable state snapshot
/// that represents the Hypergrid at a point in time, including CSR topology
/// encoding, activation management, and delta computation.
/// </summary>
public sealed class SimulationStateTuringTests
{
    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Should_create_valid_state()
    {
        var state = new SimulationState(
            activations: [1.0, 2.0, 3.0],
            edgeRowPtr: [0, 1, 2, 2],
            edgeTargets: [2, 0],
            edgeWeights: [0.5, 1.0],
            stepNumber: 0);

        state.CellCount.Should().Be(3);
        state.EdgeCount.Should().Be(2);
        state.StepNumber.Should().Be(0);
    }

    [Fact]
    public void Should_reject_mismatched_edge_row_ptr_length()
    {
        var act = () => new SimulationState(
            activations: [1.0, 2.0],
            edgeRowPtr: [0, 1],  // should be length 3 (CellCount + 1)
            edgeTargets: [0],
            edgeWeights: [1.0]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_reject_mismatched_targets_and_weights()
    {
        var act = () => new SimulationState(
            activations: [1.0],
            edgeRowPtr: [0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0, 2.0]);  // different length than targets

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_reject_null_arrays()
    {
        var act1 = () => new SimulationState(null!, [0], [], []);
        var act2 = () => new SimulationState([], null!, [], []);
        var act3 = () => new SimulationState([], [0], null!, []);
        var act4 = () => new SimulationState([], [0], [], null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    // ── WithActivations ─────────────────────────────────────────────────

    [Fact]
    public void WithActivations_should_preserve_topology()
    {
        var original = new SimulationState(
            activations: [1.0, 2.0],
            edgeRowPtr: [0, 1, 1],
            edgeTargets: [1],
            edgeWeights: [0.5],
            stepNumber: 3);

        var updated = original.WithActivations([9.0, 8.0], 4);

        updated.Activations.Should().Equal(9.0, 8.0);
        updated.StepNumber.Should().Be(4);
        updated.EdgeRowPtr.Should().BeSameAs(original.EdgeRowPtr);
        updated.EdgeTargets.Should().BeSameAs(original.EdgeTargets);
        updated.EdgeWeights.Should().BeSameAs(original.EdgeWeights);
    }

    // ── MaxDelta ────────────────────────────────────────────────────────

    [Fact]
    public void MaxDelta_identical_states_should_be_zero()
    {
        var s1 = new SimulationState([1.0, 2.0, 3.0], [0, 0, 0, 0], [], [], 0);
        var s2 = new SimulationState([1.0, 2.0, 3.0], [0, 0, 0, 0], [], [], 1);

        s1.MaxDelta(s2).Should().Be(0.0);
    }

    [Fact]
    public void MaxDelta_should_return_largest_absolute_difference()
    {
        var s1 = new SimulationState([1.0, 5.0, 3.0], [0, 0, 0, 0], [], [], 0);
        var s2 = new SimulationState([1.0, 2.0, 3.0], [0, 0, 0, 0], [], [], 1);

        s1.MaxDelta(s2).Should().Be(3.0); // |5.0 - 2.0| = 3.0
    }

    [Fact]
    public void MaxDelta_different_cell_counts_should_throw()
    {
        var s1 = new SimulationState([1.0], [0, 0], [], [], 0);
        var s2 = new SimulationState([1.0, 2.0], [0, 0, 0], [], [], 0);

        var act = () => s1.MaxDelta(s2);
        act.Should().Throw<ArgumentException>();
    }

    // ── Empty State ─────────────────────────────────────────────────────

    [Fact]
    public void Empty_state_should_be_valid()
    {
        var state = new SimulationState([], [0], [], [], 0);
        state.CellCount.Should().Be(0);
        state.EdgeCount.Should().Be(0);
    }
}
