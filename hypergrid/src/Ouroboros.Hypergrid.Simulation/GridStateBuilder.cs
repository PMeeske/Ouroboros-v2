namespace Ouroboros.Hypergrid.Simulation;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Converts a <see cref="HypergridSpace"/> topology into a flat <see cref="SimulationState"/>
/// suitable for GPU compute. The topology is encoded as a compressed sparse row (CSR) matrix
/// representing the *incoming* edge adjacency (transposed from the directed graph), so that
/// each cell's activation can be computed from its predecessors in a single kernel pass.
/// </summary>
public static class GridStateBuilder
{
    /// <summary>
    /// Builds a SimulationState from a HypergridSpace, assigning initial activation values
    /// to each cell using the provided function.
    /// </summary>
    public static SimulationState Build(
        HypergridSpace space,
        Func<GridCell, double>? initialActivation = null)
    {
        ArgumentNullException.ThrowIfNull(space);

        // Index cells by position
        var cells = space.Cells.ToList();
        var positionToIndex = new Dictionary<GridCoordinate, int>();
        for (var i = 0; i < cells.Count; i++)
            positionToIndex[cells[i].Position] = i;

        var n = cells.Count;
        var activations = new double[n];
        var init = initialActivation ?? (_ => 0.0);

        for (var i = 0; i < n; i++)
            activations[i] = init(cells[i]);

        // Build *incoming* edge adjacency as CSR (transposed from the directed graph).
        // For each cell, we need to know which cells point TO it and with what weight.
        var incomingEdges = new List<(int Target, int Source, double Weight)>();
        foreach (var edge in space.Edges)
        {
            if (positionToIndex.TryGetValue(edge.Source, out var srcIdx) &&
                positionToIndex.TryGetValue(edge.Target, out var tgtIdx))
            {
                incomingEdges.Add((tgtIdx, srcIdx, edge.Weight));
            }
        }

        // Sort by target (row) for CSR construction
        incomingEdges.Sort((a, b) => a.Target.CompareTo(b.Target));

        var edgeRowPtr = new int[n + 1];
        var edgeTargets = new int[incomingEdges.Count]; // "targets" here = source cells
        var edgeWeights = new double[incomingEdges.Count];

        var edgeIdx = 0;
        for (var row = 0; row < n; row++)
        {
            edgeRowPtr[row] = edgeIdx;
            while (edgeIdx < incomingEdges.Count && incomingEdges[edgeIdx].Target == row)
            {
                edgeTargets[edgeIdx] = incomingEdges[edgeIdx].Source;
                edgeWeights[edgeIdx] = incomingEdges[edgeIdx].Weight;
                edgeIdx++;
            }
        }
        edgeRowPtr[n] = edgeIdx;

        return new SimulationState(activations, edgeRowPtr, edgeTargets, edgeWeights);
    }
}
