namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Analytical Eye — Iaret's logical reasoning sub-entity.
/// Operates along the causal dimension, decomposing thoughts into
/// structured logical components and identifying causal relationships.
///
/// When backed by a real environment, sends a structured analysis prompt.
/// When local, uses heuristic token/marker analysis.
/// </summary>
public sealed class AnalyticalAspect : IaretAspect
{
    protected override string SystemPrompt =>
        "You are The Analytical Eye, a logical reasoning sub-entity of Iaret. " +
        "Decompose the input into structured logical components. Identify causal relationships, " +
        "conditional logic, and interrogative patterns. Output a concise analytical summary " +
        "prefixed with [ANALYTICAL].";

    public AnalyticalAspect() : base(
        "analytical",
        "The Analytical Eye",
        primaryDimension: 2) // causal axis
    { }

    protected override async Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        if (Environment is LocalIaretEnvironment)
            return TransformLocal(input, position);

        var response = await CallEnvironmentAsync(input, position, ct);
        return $"[ANALYTICAL@{position}] {response}";
    }

    protected override string TransformLocal(string input, GridCoordinate position)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var avgWordLen = words.Length > 0 ? words.Average(w => w.Length) : 0;

        var hasCausal = input.Contains("because", StringComparison.OrdinalIgnoreCase) ||
                        input.Contains("therefore", StringComparison.OrdinalIgnoreCase) ||
                        input.Contains("thus", StringComparison.OrdinalIgnoreCase);

        var hasConditional = input.Contains("if", StringComparison.OrdinalIgnoreCase) ||
                             input.Contains("when", StringComparison.OrdinalIgnoreCase);

        var hasQuestion = input.Contains('?');

        return $"[ANALYTICAL@{position}] " +
            $"tokens={wordCount} unique={uniqueWords} avg_len={avgWordLen:F1} " +
            $"causal={hasCausal} conditional={hasConditional} interrogative={hasQuestion} | " +
            input;
    }
}
