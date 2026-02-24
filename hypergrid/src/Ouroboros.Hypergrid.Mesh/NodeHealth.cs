namespace Ouroboros.Hypergrid.Mesh;

/// <summary>
/// Health status for a single Ouroboros node in the mesh.
/// </summary>
public sealed record NodeHealth(string NodeId)
{
    public NodeStatus Status { get; init; } = NodeStatus.Unknown;
    public string? StatusReason { get; init; }
    public DateTimeOffset LastHeartbeat { get; init; } = DateTimeOffset.MinValue;
    public long ProcessedCount { get; init; }
    public long ErrorCount { get; init; }
}

public enum NodeStatus
{
    Unknown,
    Healthy,
    Degraded,
    Faulted
}
