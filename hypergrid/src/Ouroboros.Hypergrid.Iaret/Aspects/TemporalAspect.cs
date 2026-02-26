namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Temporal Weaver — Iaret's memory and sequencing sub-entity.
/// Maintains a sliding window of recent thoughts, enabling temporal
/// context awareness. Tags each thought with its position in the
/// reasoning sequence and references to prior context.
/// </summary>
public sealed class TemporalAspect : IaretAspect
{
    private readonly int _windowSize;
    private readonly Queue<string> _recentThoughts;

    /// <summary>The current temporal context window.</summary>
    public IReadOnlyCollection<string> Context => _recentThoughts;

    public TemporalAspect(int windowSize = 5) : base(
        "temporal",
        "The Temporal Weaver",
        primaryDimension: 0) // temporal axis
    {
        _windowSize = windowSize;
        _recentThoughts = new Queue<string>(windowSize + 1);
    }

    protected override string Transform(string input, GridCoordinate position)
    {
        // Add to temporal window
        _recentThoughts.Enqueue(input);
        if (_recentThoughts.Count > _windowSize)
            _recentThoughts.Dequeue();

        var sequencePos = ProcessedCount + 1; // 1-based
        var contextSize = _recentThoughts.Count;
        var hasHistory = contextSize > 1;

        var temporalTag = hasHistory
            ? $"[TEMPORAL@{position}] step={sequencePos} context={contextSize}/{_windowSize} " +
              $"prior=\"{Truncate(_recentThoughts.ElementAt(contextSize - 2), 30)}\" | {input}"
            : $"[TEMPORAL@{position}] step={sequencePos} context={contextSize}/{_windowSize} (initial) | {input}";

        return temporalTag;
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
