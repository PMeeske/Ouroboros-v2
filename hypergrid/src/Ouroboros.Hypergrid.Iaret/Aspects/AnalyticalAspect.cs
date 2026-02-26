namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Analytical Eye — Iaret's logical reasoning sub-entity.
/// Operates along the causal dimension, decomposing thoughts into
/// structured logical components and identifying causal relationships.
/// </summary>
public sealed class AnalyticalAspect : IaretAspect
{
    public AnalyticalAspect() : base(
        "analytical",
        "The Analytical Eye",
        primaryDimension: 2) // causal axis
    { }

    protected override string Transform(string input, GridCoordinate position)
    {
        // Decompose into analytical components
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var avgWordLen = words.Length > 0 ? words.Average(w => w.Length) : 0;

        // Identify structural markers
        var hasCausal = input.Contains("because", StringComparison.OrdinalIgnoreCase) ||
                        input.Contains("therefore", StringComparison.OrdinalIgnoreCase) ||
                        input.Contains("thus", StringComparison.OrdinalIgnoreCase);

        var hasConditional = input.Contains("if", StringComparison.OrdinalIgnoreCase) ||
                             input.Contains("when", StringComparison.OrdinalIgnoreCase);

        var hasQuestion = input.Contains('?');

        var analysis = $"[ANALYTICAL@{position}] " +
            $"tokens={wordCount} unique={uniqueWords} avg_len={avgWordLen:F1} " +
            $"causal={hasCausal} conditional={hasConditional} interrogative={hasQuestion} | " +
            input;

        return analysis;
    }
}
