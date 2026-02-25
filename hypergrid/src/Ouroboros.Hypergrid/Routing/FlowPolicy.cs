namespace Ouroboros.Hypergrid.Routing;

/// <summary>
/// Routing policy that governs how thought streams propagate through the grid.
/// </summary>
public sealed record FlowPolicy
{
    public FlowStrategy Strategy { get; init; } = FlowStrategy.Broadcast;

    /// <summary>
    /// When <see cref="Strategy"/> is <see cref="FlowStrategy.Dimensional"/>,
    /// streams are routed only along this dimension.
    /// </summary>
    public int PreferredDimension { get; init; }

    public static FlowPolicy Broadcast => new() { Strategy = FlowStrategy.Broadcast };
    public static FlowPolicy Nearest => new() { Strategy = FlowStrategy.Nearest };
    public static FlowPolicy ForDimension(int dim) => new() { Strategy = FlowStrategy.Dimensional, PreferredDimension = dim };
}

public enum FlowStrategy
{
    Broadcast,
    Nearest,
    Dimensional
}
