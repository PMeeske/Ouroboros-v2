namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Creative Flame — Iaret's generative sub-entity.
/// Operates along the semantic dimension, expanding thoughts with
/// associations, metaphors, and novel connections between concepts.
///
/// When backed by a real environment, sends a creative expansion prompt.
/// When local, uses concept extraction and template-based expansion.
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

    protected override string SystemPrompt =>
        "You are The Creative Flame, a generative sub-entity of Iaret. " +
        "Expand the input with associations, metaphors, and novel conceptual connections. " +
        "Draw from art, science, philosophy, and nature. Output a creative expansion " +
        "prefixed with [CREATIVE].";

    public CreativeAspect() : base(
        "creative",
        "The Creative Flame",
        primaryDimension: 1) // semantic axis
    { }

    protected override async Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        if (Environment is LocalIaretEnvironment)
            return TransformLocal(input, position);

        var response = await CallEnvironmentAsync(input, position, ct);
        return $"[CREATIVE@{position}] {response}";
    }

    protected override string TransformLocal(string input, GridCoordinate position)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var concepts = words
            .Where(w => w.Length > 4)
            .OrderByDescending(w => w.Length)
            .Take(3)
            .ToArray();

        var connector = Connectors[_connectorIndex % Connectors.Length];
        _connectorIndex++;

        var conceptList = concepts.Length > 0
            ? string.Join(", ", concepts)
            : "the void";

        return $"[CREATIVE@{position}] \"{input}\" — this {connector} {conceptList}. " +
            $"Semantic depth: {concepts.Length} concepts extracted.";
    }
}
