namespace Ouroboros.Hypergrid.Simulation;

/// <summary>
/// Abstraction for grid simulation backends (GPU via OpenCL/ILGPU or CPU fallback).
/// The simulator propagates activation values through the Hypergrid topology in parallel,
/// computing a full step of thought propagation across all cells simultaneously.
/// </summary>
public interface IGridSimulator : IDisposable
{
    /// <summary>
    /// Runs a single propagation step: each cell's activation is updated based on
    /// its incoming edge weights and neighbor activations.
    /// </summary>
    SimulationState Step(SimulationState state);

    /// <summary>
    /// Runs multiple propagation steps until convergence or max iterations.
    /// Returns the final state and the number of steps taken.
    /// </summary>
    (SimulationState FinalState, int Steps) RunUntilConvergence(
        SimulationState initial,
        double convergenceThreshold = 1e-6,
        int maxSteps = 1000);

    /// <summary>Human-readable name of the compute backend (e.g., "OpenCL", "CPU").</summary>
    string BackendName { get; }
}
