namespace Ouroboros.Hypergrid.Simulation;

/// <summary>
/// CPU-based grid simulator — the fallback when no GPU/OpenCL device is available.
/// Implements the same propagation algorithm as the GPU kernel, using SIMD-friendly
/// array operations. Each cell's new activation is the weighted sum of incoming
/// neighbor activations passed through an activation function (tanh by default).
/// </summary>
public sealed class CpuGridSimulator : IGridSimulator
{
    private readonly ActivationFunction _activation;

    public string BackendName => "CPU";

    public CpuGridSimulator(ActivationFunction? activation = null)
    {
        _activation = activation ?? ActivationFunctions.Tanh;
    }

    public SimulationState Step(SimulationState state)
    {
        var n = state.CellCount;
        var newActivations = new double[n];

        // For each cell, compute weighted sum of incoming activations
        for (var i = 0; i < n; i++)
        {
            var start = state.EdgeRowPtr[i];
            var end = state.EdgeRowPtr[i + 1];

            if (start == end)
            {
                // No incoming edges — retain current activation
                newActivations[i] = state.Activations[i];
                continue;
            }

            var sum = 0.0;
            for (var e = start; e < end; e++)
            {
                var srcIdx = state.EdgeTargets[e];
                var weight = state.EdgeWeights[e];
                sum += state.Activations[srcIdx] * weight;
            }

            newActivations[i] = _activation(sum);
        }

        return state.WithActivations(newActivations, state.StepNumber + 1);
    }

    public (SimulationState FinalState, int Steps) RunUntilConvergence(
        SimulationState initial,
        double convergenceThreshold = 1e-6,
        int maxSteps = 1000)
    {
        var current = initial;

        for (var step = 0; step < maxSteps; step++)
        {
            var next = Step(current);

            if (next.MaxDelta(current) < convergenceThreshold)
                return (next, step + 1);

            current = next;
        }

        return (current, maxSteps);
    }

    public void Dispose()
    {
        // No resources to release for CPU backend
    }
}
