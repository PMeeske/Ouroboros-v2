namespace Ouroboros.Hypergrid.Mesh;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Wraps an Ouroboros pipeline instance as a node in the Hypergrid mesh.
/// Each node sits at a grid vertex and can process/route thought streams.
/// </summary>
public sealed class OuroborosNode
{
    public string Id { get; }
    public GridCoordinate Position { get; }
    public NodeHealth Health { get; private set; }

    public OuroborosNode(string id, GridCoordinate position)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Health = new NodeHealth(id);
    }

    public void ReportHealthy() => Health = Health with { Status = NodeStatus.Healthy, LastHeartbeat = DateTimeOffset.UtcNow };
    public void ReportDegraded(string reason) => Health = Health with { Status = NodeStatus.Degraded, StatusReason = reason, LastHeartbeat = DateTimeOffset.UtcNow };
    public void ReportFaulted(string reason) => Health = Health with { Status = NodeStatus.Faulted, StatusReason = reason, LastHeartbeat = DateTimeOffset.UtcNow };
}
