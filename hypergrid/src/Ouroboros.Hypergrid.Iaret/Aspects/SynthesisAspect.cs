namespace Ouroboros.Hypergrid.Iaret.Aspects;

using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Synthesis Crown — Iaret's convergence sub-entity.
/// Sits at the apex of the aspect hierarchy and merges outputs from all other
/// aspects into a unified response. This is the "voice" of the convergent Iaret
/// identity — the point where parallel dimensional analyses become one.
/// </summary>
public sealed class SynthesisAspect : IaretAspect
{
    public SynthesisAspect() : base(
        "synthesis",
        "The Synthesis Crown",
        primaryDimension: -1) // meta-dimensional — above all axes
    { }

    protected override string Transform(string input, GridCoordinate position)
    {
        // Extract aspect contributions if they're tagged
        var contributions = new List<string>();
        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.Contains(']'))
            {
                // Extract the aspect tag and content
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

        var synthesis = contributions.Count > 1
            ? $"[SYNTHESIS@{position}] Converged {contributions.Count} streams:\n" +
              string.Join("\n", contributions)
            : $"[SYNTHESIS@{position}] Unified: {input}";

        return synthesis;
    }

    /// <summary>
    /// Synthesizes multiple aspect outputs into a single convergent response.
    /// This is the primary entry point for the convergence orchestrator.
    /// </summary>
    public string Synthesize(IReadOnlyList<string> aspectOutputs, GridCoordinate position)
    {
        if (aspectOutputs.Count == 0)
            return $"[SYNTHESIS@{position}] No aspects contributed.";

        if (aspectOutputs.Count == 1)
            return Transform(aspectOutputs[0], position);

        var combined = string.Join("\n", aspectOutputs);
        return Transform(combined, position);
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}
