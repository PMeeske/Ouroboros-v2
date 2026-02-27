namespace Ouroboros.Hypergrid.Cli.Providers;

using System.Collections.Concurrent;
using Ouroboros.Hypergrid.Host;

/// <summary>
/// Maps model name prefixes to <see cref="IOuroborosPipeline"/> backend instances.
///
/// Resolution order:
///   1. Exact match on model name (e.g., "iaret")
///   2. Prefix match with "/" separator (e.g., "ollama/llama3" → "ollama" provider)
///   3. Default provider (the first registered, typically "iaret")
/// </summary>
public sealed class PipelineProviderRegistry
{
    private readonly ConcurrentDictionary<string, IOuroborosPipeline> _providers = new(StringComparer.OrdinalIgnoreCase);
    private string? _defaultProvider;

    /// <summary>Register a provider under the given prefix. First registered becomes default.</summary>
    public void Register(string prefix, IOuroborosPipeline pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(pipeline);
        _providers[prefix] = pipeline;
        _defaultProvider ??= prefix;
    }

    /// <summary>
    /// Resolve a model string to a pipeline and the effective model name.
    /// Returns null if no provider matches and no default is registered.
    /// </summary>
    public (IOuroborosPipeline Pipeline, string? EffectiveModel)? Resolve(string model)
    {
        // 1. Exact match — return null effectiveModel so the pipeline uses its own default
        if (_providers.TryGetValue(model, out var exact))
            return (exact, null);

        // 2. Prefix match: "prefix/submodel" — return the sub-model as override
        var slashIndex = model.IndexOf('/');
        if (slashIndex > 0)
        {
            var prefix = model[..slashIndex];
            if (_providers.TryGetValue(prefix, out var prefixed))
                return (prefixed, model[(slashIndex + 1)..]);
        }

        // 3. Default
        if (_defaultProvider is not null && _providers.TryGetValue(_defaultProvider, out var fallback))
            return (fallback, null);

        return null;
    }

    /// <summary>All registered provider prefixes and their pipeline model names.</summary>
    public IEnumerable<(string Prefix, string ModelName)> ListProviders()
        => _providers.Select(kvp => (kvp.Key, kvp.Value.ModelName));
}
