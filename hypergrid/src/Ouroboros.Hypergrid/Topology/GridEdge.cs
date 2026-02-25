namespace Ouroboros.Hypergrid.Topology;

/// <summary>
/// A directed edge between two vertices in the Hypergrid,
/// carrying metadata about the dimension it traverses.
/// </summary>
public sealed record GridEdge(
    GridCoordinate Source,
    GridCoordinate Target,
    int Dimension,
    string? Label = null)
{
    /// <summary>Weight / cost of traversing this edge (default 1.0).</summary>
    public double Weight { get; init; } = 1.0;
}
