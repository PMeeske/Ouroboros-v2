namespace Ouroboros.Hypergrid.Mesh;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Interwiring protocol — creates cross-connections between Ouroboros nodes
/// across grid edges, enabling distributed thought propagation.
/// </summary>
public interface IInterwire
{
    /// <summary>
    /// Establishes a stream connection between two nodes along the given edge.
    /// </summary>
    Task<StreamConnection> Connect(
        OuroborosNode source,
        OuroborosNode target,
        GridEdge edge,
        CancellationToken ct);

    /// <summary>
    /// Disconnects a previously established connection.
    /// </summary>
    Task Disconnect(StreamConnection connection, CancellationToken ct);
}

/// <summary>
/// Represents an active connection between two Ouroboros nodes.
/// </summary>
public sealed record StreamConnection(
    string ConnectionId,
    OuroborosNode Source,
    OuroborosNode Target,
    GridEdge Edge)
{
    public DateTimeOffset EstablishedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; init; } = true;
}
