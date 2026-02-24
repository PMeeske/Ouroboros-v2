namespace Ouroboros.Hypergrid.Topology;

/// <summary>
/// An N-dimensional coordinate identifying a position within the Hypergrid.
/// Each component maps to a named dimension (temporal, semantic, causal, modal, ...).
/// </summary>
public sealed record GridCoordinate
{
    public IReadOnlyList<int> Components { get; }
    public int Rank => Components.Count;

    public GridCoordinate(params int[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Length == 0)
            throw new ArgumentException("A coordinate must have at least one component.", nameof(components));

        Components = components.ToArray();
    }

    public int this[int dimension] => Components[dimension];

    /// <summary>Manhattan distance between two coordinates of equal rank.</summary>
    public int ManhattanDistance(GridCoordinate other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Rank != other.Rank)
            throw new ArgumentException("Coordinates must have equal rank.", nameof(other));

        return Components.Zip(other.Components, (a, b) => Math.Abs(a - b)).Sum();
    }

    public override string ToString() =>
        $"({string.Join(", ", Components)})";
}
