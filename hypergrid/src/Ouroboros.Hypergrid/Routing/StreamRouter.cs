namespace Ouroboros.Hypergrid.Routing;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Routes thought streams through the Hypergrid based on a <see cref="FlowPolicy"/>.
/// </summary>
public sealed class StreamRouter
{
    private readonly HypergridSpace _space;
    private readonly FlowPolicy _policy;

    public StreamRouter(HypergridSpace space, FlowPolicy policy)
    {
        _space = space ?? throw new ArgumentNullException(nameof(space));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>
    /// Determines the next set of target coordinates for a stream originating at <paramref name="from"/>.
    /// </summary>
    public IReadOnlyList<GridCoordinate> ResolveTargets(GridCoordinate from) =>
        _policy.Strategy switch
        {
            FlowStrategy.Broadcast => _space.GetEdgesFrom(from).Select(e => e.Target).ToList(),
            FlowStrategy.Nearest => _space.GetEdgesFrom(from)
                .OrderBy(e => e.Weight)
                .Take(1)
                .Select(e => e.Target)
                .ToList(),
            FlowStrategy.Dimensional => _space.GetEdgesFrom(from)
                .Where(e => e.Dimension == _policy.PreferredDimension)
                .Select(e => e.Target)
                .ToList(),
            _ => []
        };
}
