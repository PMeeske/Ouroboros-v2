namespace Ouroboros.Hypergrid.Topology;

/// <summary>
/// Defines an N-dimensional grid space with named dimensions and a collection of cells.
/// This is the root topology object for the Hypergrid.
/// </summary>
public sealed class HypergridSpace
{
    private readonly Dictionary<GridCoordinate, GridCell> _cells = new();
    private readonly List<GridEdge> _edges = new();

    public IReadOnlyList<DimensionDescriptor> Dimensions { get; }
    public int Rank => Dimensions.Count;
    public IReadOnlyCollection<GridCell> Cells => _cells.Values;
    public IReadOnlyList<GridEdge> Edges => _edges;

    public HypergridSpace(IEnumerable<DimensionDescriptor> dimensions)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        Dimensions = dimensions.ToList();
        if (Dimensions.Count == 0)
            throw new ArgumentException("A hypergrid must have at least one dimension.", nameof(dimensions));
    }

    public GridCell AddCell(GridCoordinate position, string nodeId)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (position.Rank != Rank)
            throw new ArgumentException($"Coordinate rank {position.Rank} does not match grid rank {Rank}.", nameof(position));

        var cell = new GridCell(position, nodeId);
        _cells[position] = cell;
        return cell;
    }

    public GridEdge Connect(GridCoordinate source, GridCoordinate target, int dimension, string? label = null)
    {
        var edge = new GridEdge(source, target, dimension, label);
        _edges.Add(edge);
        return edge;
    }

    public GridCell? GetCell(GridCoordinate position) =>
        _cells.GetValueOrDefault(position);

    public IEnumerable<GridEdge> GetEdgesFrom(GridCoordinate position) =>
        _edges.Where(e => e.Source == position);
}

/// <summary>Describes a single dimension in the Hypergrid.</summary>
public sealed record DimensionDescriptor(int Index, string Name, string Description);
