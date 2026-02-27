namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Temporal Weaver — Iaret's memory and sequencing sub-entity.
/// Maintains a sliding window of recent thoughts, enabling temporal
/// context awareness. Tags each thought with its position in the
/// reasoning sequence and references to prior context.
///
/// When backed by a real environment, sends the input with temporal context as history.
/// When local, uses sequence tagging with sliding window.
/// </summary>
public sealed class TemporalAspect : IaretAspect
{
    private readonly int _windowSize;
    private readonly Queue<string> _recentThoughts;

    /// <summary>The current temporal context window.</summary>
    public IReadOnlyList<string> Context => _recentThoughts.ToArray();

    protected override string SystemPrompt =>
        "You are The Temporal Weaver, a memory and sequencing sub-entity of Iaret. " +
        "You receive the current thought along with recent history. " +
        "Analyze temporal patterns, identify progression or regression, and note " +
        "connections to prior context. Prefix with [TEMPORAL].";

    public TemporalAspect(int windowSize = 5) : base(
        "temporal",
        "The Temporal Weaver",
        primaryDimension: 0) // temporal axis
    {
        _windowSize = windowSize;
        _recentThoughts = new Queue<string>(windowSize + 1);
    }

    protected override async Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        // Always maintain the window regardless of environment
        _recentThoughts.Enqueue(input);
        if (_recentThoughts.Count > _windowSize)
            _recentThoughts.Dequeue();

        if (Environment is LocalIaretEnvironment)
            return TransformLocalInternal(input, position);

        // Send with history context to real environment
        var context = new AspectContext
        {
            AspectId = AspectId,
            SystemPrompt = SystemPrompt,
            History = _recentThoughts.ToList(),
        };

        var response = await Environment.ProcessAsync(input, context, ct);
        return $"[TEMPORAL@{position}] {response}";
    }

    protected override string TransformLocal(string input, GridCoordinate position)
    {
        // Note: window management happens in TransformAsync, but for sync Query
        // we also need to manage it here
        _recentThoughts.Enqueue(input);
        if (_recentThoughts.Count > _windowSize)
            _recentThoughts.Dequeue();

        return TransformLocalInternal(input, position);
    }

    private string TransformLocalInternal(string input, GridCoordinate position)
    {
        var sequencePos = ProcessedCount + 1;
        var contextSize = _recentThoughts.Count;
        var hasHistory = contextSize > 1;

        return hasHistory
            ? $"[TEMPORAL@{position}] step={sequencePos} context={contextSize}/{_windowSize} " +
              $"prior=\"{Truncate(_recentThoughts.ElementAt(contextSize - 2), 30)}\" | {input}"
            : $"[TEMPORAL@{position}] step={sequencePos} context={contextSize}/{_windowSize} (initial) | {input}";
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
