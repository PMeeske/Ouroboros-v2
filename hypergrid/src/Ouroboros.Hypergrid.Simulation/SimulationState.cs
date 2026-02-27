namespace Ouroboros.Hypergrid.Simulation;

/// <summary>
/// Immutable snapshot of the Hypergrid simulation at a point in time.
/// Contains flat arrays suitable for GPU transfer — the topology is encoded
/// as a compressed sparse row (CSR) adjacency structure.
/// </summary>
public sealed class SimulationState
{
    /// <summary>Activation level for each cell (indexed 0..N-1).</summary>
    public double[] Activations { get; }

    /// <summary>Number of cells in the grid.</summary>
    public int CellCount => Activations.Length;

    /// <summary>
    /// CSR row pointers: EdgeRowPtr[i] is the index into EdgeTargets/EdgeWeights
    /// where cell i's outgoing edges begin. EdgeRowPtr[N] = total edge count.
    /// </summary>
    public int[] EdgeRowPtr { get; }

    /// <summary>CSR column indices: target cell index for each edge.</summary>
    public int[] EdgeTargets { get; }

    /// <summary>CSR values: weight for each edge.</summary>
    public double[] EdgeWeights { get; }

    /// <summary>Total number of edges.</summary>
    public int EdgeCount => EdgeTargets.Length;

    /// <summary>Simulation step number.</summary>
    public int StepNumber { get; }

    public SimulationState(
        double[] activations,
        int[] edgeRowPtr,
        int[] edgeTargets,
        double[] edgeWeights,
        int stepNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentNullException.ThrowIfNull(edgeRowPtr);
        ArgumentNullException.ThrowIfNull(edgeTargets);
        ArgumentNullException.ThrowIfNull(edgeWeights);

        if (edgeRowPtr.Length != activations.Length + 1)
            throw new ArgumentException(
                $"EdgeRowPtr length ({edgeRowPtr.Length}) must be CellCount+1 ({activations.Length + 1}).");

        if (edgeTargets.Length != edgeWeights.Length)
            throw new ArgumentException("EdgeTargets and EdgeWeights must have equal length.");

        Activations = activations;
        EdgeRowPtr = edgeRowPtr;
        EdgeTargets = edgeTargets;
        EdgeWeights = edgeWeights;
        StepNumber = stepNumber;
    }

    /// <summary>Creates a new state with updated activations (topology unchanged).</summary>
    public SimulationState WithActivations(double[] newActivations, int newStep) =>
        new(newActivations, EdgeRowPtr, EdgeTargets, EdgeWeights, newStep);

    /// <summary>Computes the maximum absolute activation change between this and another state.</summary>
    public double MaxDelta(SimulationState other)
    {
        if (CellCount != other.CellCount)
            throw new ArgumentException("States must have equal cell count.");

        var max = 0.0;
        for (var i = 0; i < CellCount; i++)
        {
            var delta = Math.Abs(Activations[i] - other.Activations[i]);
            if (delta > max) max = delta;
        }
        return max;
    }
}
