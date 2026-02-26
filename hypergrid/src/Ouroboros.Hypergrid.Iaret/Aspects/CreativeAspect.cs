namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Creative Flame — Iaret's generative sub-entity.
/// Operates along the semantic dimension, expanding thoughts with
/// associations, metaphors, and novel connections between concepts.
/// </summary>
public sealed class CreativeAspect : IaretAspect
{
    private static readonly string[] Connectors =
    [
        "reminds me of", "echoes", "resonates with",
        "mirrors", "dances alongside", "weaves into",
        "spirals toward", "blooms from"
    ];

    private int _connectorIndex;

    public CreativeAspect() : base(
        "creative",
        "The Creative Flame",
        primaryDimension: 1) // semantic axis
    { }

    protected override string Transform(string input, GridCoordinate position)
    {
        // Extract key concepts (simple: longest words as "concepts")
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var concepts = words
            .Where(w => w.Length > 4)
            .OrderByDescending(w => w.Length)
            .Take(3)
            .ToArray();

        // Generate a creative expansion
        var connector = Connectors[_connectorIndex % Connectors.Length];
        _connectorIndex++;

        var conceptList = concepts.Length > 0
            ? string.Join(", ", concepts)
            : "the void";

        return $"[CREATIVE@{position}] \"{input}\" — this {connector} {conceptList}. " +
            $"Semantic depth: {concepts.Length} concepts extracted.";
    }
}
