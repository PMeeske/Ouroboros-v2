namespace Ouroboros.Hypergrid.Mesh;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// CLI-driven orchestrator that manages the lifecycle of the Ouroboros mesh:
/// node registration, interwiring, health monitoring, and graceful shutdown.
/// </summary>
public sealed class MeshOrchestrator
{
    private readonly HypergridSpace _space;
    private readonly IInterwire _interwire;
    private readonly Dictionary<string, OuroborosNode> _nodes = new();
    private readonly List<StreamConnection> _connections = new();

    public IReadOnlyCollection<OuroborosNode> Nodes => _nodes.Values;
    public IReadOnlyList<StreamConnection> Connections => _connections;

    public MeshOrchestrator(HypergridSpace space, IInterwire interwire)
    {
        _space = space ?? throw new ArgumentNullException(nameof(space));
        _interwire = interwire ?? throw new ArgumentNullException(nameof(interwire));
    }

    /// <summary>Registers a new Ouroboros node at the given grid position.</summary>
    public OuroborosNode Register(string nodeId, GridCoordinate position)
    {
        var node = new OuroborosNode(nodeId, position);
        _nodes[nodeId] = node;
        _space.AddCell(position, nodeId);
        node.ReportHealthy();
        return node;
    }

    /// <summary>Interwires two nodes along the specified dimension.</summary>
    public async Task<StreamConnection> Interwire(string sourceId, string targetId, int dimension, CancellationToken ct = default)
    {
        var source = _nodes[sourceId];
        var target = _nodes[targetId];
        var edge = _space.Connect(source.Position, target.Position, dimension);
        var connection = await _interwire.Connect(source, target, edge, ct);
        _connections.Add(connection);
        return connection;
    }

    /// <summary>Returns health summaries for all registered nodes.</summary>
    public IReadOnlyList<NodeHealth> GetHealthReport() =>
        _nodes.Values.Select(n => n.Health).ToList();
}
