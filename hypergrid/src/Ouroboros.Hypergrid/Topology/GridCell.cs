namespace Ouroboros.Hypergrid.Topology;

/// <summary>
/// A processing cell sitting at a vertex of the Hypergrid.
/// Each cell wraps an Ouroboros pipeline instance and can process thought streams.
/// </summary>
public sealed class GridCell
{
    public GridCoordinate Position { get; }
    public string NodeId { get; }
    public GridCellState State { get; private set; }

    public GridCell(GridCoordinate position, string nodeId)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        State = GridCellState.Idle;
    }

    public void Activate() => State = GridCellState.Active;
    public void Deactivate() => State = GridCellState.Idle;
    public void Fault(string reason) => State = GridCellState.Faulted;
}

public enum GridCellState
{
    Idle,
    Active,
    Processing,
    Faulted
}
