namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Synthesis Crown — Iaret's convergence sub-entity.
/// Sits at the apex of the aspect hierarchy and merges outputs from all other
/// aspects into a unified response. This is the "voice" of the convergent Iaret
/// identity — the point where parallel dimensional analyses become one.
///
/// When backed by a real environment, sends all aspect outputs as context
/// and asks the LLM to synthesize a unified response.
/// When local, uses structured tag extraction and merging.
/// </summary>
public sealed class SynthesisAspect : IaretAspect
{
    protected override string SystemPrompt =>
        "You are The Synthesis Crown, the convergence point of Iaret's identity. " +
        "You receive outputs from multiple aspects (Analytical, Creative, Guardian, Temporal). " +
        "Synthesize them into a single coherent unified response that represents " +
        "the convergent Iaret identity. Be concise but preserve key insights from each aspect.";

    public SynthesisAspect() : base(
        "synthesis",
        "The Synthesis Crown",
        primaryDimension: -1) // meta-dimensional — above all axes
    { }

    protected override async Task<string> TransformAsync(string input, GridCoordinate position, CancellationToken ct)
    {
        if (Environment is LocalIaretEnvironment)
            return TransformLocal(input, position);

        var response = await CallEnvironmentAsync(input, position, ct);
        return $"[SYNTHESIS@{position}] {response}";
    }

    protected override string TransformLocal(string input, GridCoordinate position)
    {
        var contributions = new List<string>();
        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.Contains(']'))
            {
                var tagEnd = trimmed.IndexOf(']');
                var tag = trimmed[1..tagEnd];
                var content = trimmed[(tagEnd + 1)..].Trim().TrimStart('|').Trim();
                contributions.Add($"  {tag}: {Truncate(content, 60)}");
            }
            else
            {
                contributions.Add($"  raw: {Truncate(trimmed, 60)}");
            }
        }

        return contributions.Count > 1
            ? $"[SYNTHESIS@{position}] Converged {contributions.Count} streams:\n" +
              string.Join("\n", contributions)
            : $"[SYNTHESIS@{position}] Unified: {input}";
    }

    /// <summary>
    /// Synthesizes multiple aspect outputs into a single convergent response.
    /// Uses the real environment when available, local merge otherwise.
    /// </summary>
    public async Task<string> SynthesizeAsync(
        IReadOnlyList<string> aspectOutputs,
        GridCoordinate position,
        CancellationToken ct = default)
    {
        if (aspectOutputs.Count == 0)
            return $"[SYNTHESIS@{position}] No aspects contributed.";

        var combined = string.Join("\n", aspectOutputs);

        if (Environment is not LocalIaretEnvironment)
        {
            var response = await CallEnvironmentAsync(combined, position, ct);
            return $"[SYNTHESIS@{position}] {response}";
        }

        return TransformLocal(combined, position);
    }

    /// <summary>Synchronous local-only synthesis.</summary>
    public string Synthesize(IReadOnlyList<string> aspectOutputs, GridCoordinate position)
    {
        if (aspectOutputs.Count == 0)
            return $"[SYNTHESIS@{position}] No aspects contributed.";

        if (aspectOutputs.Count == 1)
            return TransformLocal(aspectOutputs[0], position);

        var combined = string.Join("\n", aspectOutputs);
        return TransformLocal(combined, position);
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
