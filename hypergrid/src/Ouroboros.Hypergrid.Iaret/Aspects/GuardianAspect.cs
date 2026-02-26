namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Guardian Uraeus — Iaret's protective sub-entity.
/// Operates along the temporal dimension, evaluating thought safety,
/// coherence, and alignment. Filters or annotates thoughts that
/// fail validation, ensuring the convergent identity remains stable.
/// </summary>
public sealed class GuardianAspect : IaretAspect
{
    private readonly double _coherenceThreshold;
    private long _blockedCount;

    public long BlockedCount => _blockedCount;

    public GuardianAspect(double coherenceThreshold = 0.3) : base(
        "guardian",
        "The Guardian Uraeus",
        primaryDimension: 0) // temporal axis — guards the flow of time
    {
        _coherenceThreshold = coherenceThreshold;
    }

    protected override bool ShouldProcess(string payload) =>
        !string.IsNullOrWhiteSpace(payload);

    protected override string Transform(string input, GridCoordinate position)
    {
        var coherence = ComputeCoherence(input);
        var safe = coherence >= _coherenceThreshold;

        if (!safe)
        {
            Interlocked.Increment(ref _blockedCount);
            return $"[GUARDIAN@{position}] BLOCKED (coherence={coherence:F2} < {_coherenceThreshold:F2}): {Truncate(input, 50)}";
        }

        return $"[GUARDIAN@{position}] PASSED (coherence={coherence:F2}): {input}";
    }

    private static double ComputeCoherence(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0.0;

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return 0.0;

        // Simple coherence heuristic: ratio of meaningful words to total,
        // combined with average word length (proxy for vocabulary sophistication)
        var meaningful = words.Count(w => w.Length > 2);
        var meaningfulRatio = (double)meaningful / words.Length;
        var avgLen = words.Average(w => w.Length);
        var lenScore = Math.Min(avgLen / 8.0, 1.0);

        return (meaningfulRatio * 0.6) + (lenScore * 0.4);
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
