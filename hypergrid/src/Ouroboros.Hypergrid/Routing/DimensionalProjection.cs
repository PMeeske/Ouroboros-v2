namespace Ouroboros.Hypergrid.Routing;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Projects a thought stream from one dimension to another,
/// enabling cross-dimensional reasoning (e.g. temporal → causal).
/// </summary>
public static class DimensionalProjection
{
    /// <summary>
    /// Creates a new coordinate by replacing the value at <paramref name="targetDimension"/>
    /// with <paramref name="newValue"/>, preserving all other dimensions.
    /// </summary>
    public static GridCoordinate Project(GridCoordinate source, int targetDimension, int newValue)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (targetDimension < 0 || targetDimension >= source.Rank)
            throw new ArgumentOutOfRangeException(nameof(targetDimension));

        var components = source.Components.ToArray();
        components[targetDimension] = newValue;
        return new GridCoordinate(components);
    }

    /// <summary>
    /// Returns a slice of coordinates along a single dimension, holding all others constant.
    /// </summary>
    public static IEnumerable<GridCoordinate> Slice(GridCoordinate origin, int dimension, int from, int to)
    {
        for (var i = from; i <= to; i++)
            yield return Project(origin, dimension, i);
    }
}
