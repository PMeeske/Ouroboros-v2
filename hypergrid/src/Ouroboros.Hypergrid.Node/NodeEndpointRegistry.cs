namespace Ouroboros.Hypergrid.Node;

using System.Collections.Concurrent;

/// <summary>
/// Maps node IDs to their HTTP base URLs for inter-container communication.
/// Populated from the <c>MESH_PEERS</c> environment variable at startup.
/// </summary>
public sealed class NodeEndpointRegistry
{
    private readonly ConcurrentDictionary<string, Uri> _endpoints = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a peer node's endpoint URL.
    /// </summary>
    public void Register(string nodeId, Uri baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(baseUrl);
        _endpoints[nodeId] = baseUrl;
    }

    /// <summary>
    /// Resolve a node ID to its base URL.
    /// </summary>
    public Uri GetEndpoint(string nodeId)
    {
        if (_endpoints.TryGetValue(nodeId, out var url))
            return url;
        throw new KeyNotFoundException($"No endpoint registered for node '{nodeId}'.");
    }

    /// <summary>
    /// Try to resolve a node ID. Returns false if not registered.
    /// </summary>
    public bool TryGetEndpoint(string nodeId, out Uri? baseUrl)
        => _endpoints.TryGetValue(nodeId, out baseUrl);

    /// <summary>All registered node IDs.</summary>
    public IReadOnlyCollection<string> RegisteredNodes => _endpoints.Keys.ToArray();

    /// <summary>
    /// Parse the <c>MESH_PEERS</c> environment variable format:
    /// <c>nodeId=http://host:port,nodeId=http://host:port</c>
    /// or simple URL-only format where the hostname becomes the node ID:
    /// <c>http://iaret-beta:9500,http://iaret-gamma:9500</c>
    /// </summary>
    public static NodeEndpointRegistry FromEnvironment(string? meshPeers)
    {
        var registry = new NodeEndpointRegistry();
        if (string.IsNullOrWhiteSpace(meshPeers))
            return registry;

        foreach (var entry in meshPeers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (entry.Contains('='))
            {
                var parts = entry.Split('=', 2);
                registry.Register(parts[0], new Uri(parts[1]));
            }
            else
            {
                var uri = new Uri(entry);
                registry.Register(uri.Host, uri);
            }
        }

        return registry;
    }
}
