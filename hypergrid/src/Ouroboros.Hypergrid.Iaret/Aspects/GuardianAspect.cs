namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Guardian Uraeus — Iaret's protective sub-entity.
/// Operates along the temporal dimension, evaluating thought safety,
/// coherence, and alignment. Filters or annotates thoughts that
/// fail validation, ensuring the convergent identity remains stable.
///
/// When backed by a real environment, sends a safety/coherence evaluation prompt.
/// When local, uses heuristic coherence scoring.
/// </summary>
public sealed class GuardianAspect : IaretAspect
{
    private readonly double _coherenceThreshold;
    private long _blockedCount;

    public long BlockedCount => Interlocked.Read(ref _blockedCount);

    protected override string SystemPrompt =>
        "You are The Guardian Uraeus, a protective sub-entity of Iaret. " +
        "Evaluate the input for coherence, safety, and alignment. " +
        "If the input is coherent and safe, output PASSED with a brief justification. " +
        "If not, output BLOCKED with the reason. Prefix with [GUARDIAN].";

    public GuardianAspect(double coherenceThreshold = 0.3) : base(
        "guardian",
        "The Guardian Uraeus",
        primaryDimension: 0) // temporal axis — guards the flow of time
    {
        _coherenceThreshold = coherenceThreshold;
    }

    protected override bool ShouldProcess(string payload) =>
        !string.IsNullOrWhiteSpace(payload);

    protected override async Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        if (Environment is LocalIaretEnvironment)
            return TransformLocal(input, position);

        var response = await CallEnvironmentAsync(input, position, ct);

        // Track blocks from real environment too
        if (response.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _blockedCount);

        return $"[GUARDIAN@{position}] {response}";
    }

    protected override string TransformLocal(string input, GridCoordinate position)
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

        var meaningful = words.Count(w => w.Length > 2);
        var meaningfulRatio = (double)meaningful / words.Length;
        var avgLen = words.Average(w => w.Length);
        var lenScore = Math.Min(avgLen / 8.0, 1.0);

        return (meaningfulRatio * 0.6) + (lenScore * 0.4);
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
