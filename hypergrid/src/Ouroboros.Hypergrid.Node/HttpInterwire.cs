namespace Ouroboros.Hypergrid.Node;

using System.Net.Http.Json;
using Ouroboros.Hypergrid.Mesh;
using Ouroboros.Hypergrid.Topology;

/// <summary>
/// Production <see cref="IInterwire"/> implementation that establishes
/// cross-node connections via HTTP between Docker containers.
/// </summary>
public sealed class HttpInterwire : IInterwire
{
    private readonly HttpClient _http;
    private readonly NodeEndpointRegistry _registry;

    public HttpInterwire(HttpClient http, NodeEndpointRegistry registry)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<StreamConnection> Connect(
        OuroborosNode source,
        OuroborosNode target,
        GridEdge edge,
        CancellationToken ct)
    {
        var targetUrl = _registry.GetEndpoint(target.Id);
        var requestUri = new Uri(targetUrl, "/api/interwire/connect");

        var payload = new InterwireRequest(
            source.Id,
            target.Id,
            edge.Dimension,
            source.Position[0], source.Position[1], source.Position[2]);

        var response = await _http.PostAsJsonAsync(requestUri, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InterwireResponse>(ct);

        return new StreamConnection(
            result!.ConnectionId,
            source,
            target,
            edge);
    }

    public async Task Disconnect(StreamConnection connection, CancellationToken ct)
    {
        var targetUrl = _registry.GetEndpoint(connection.Target.Id);
        var requestUri = new Uri(targetUrl, "/api/interwire/disconnect");

        var payload = new DisconnectRequest(connection.ConnectionId);
        var response = await _http.PostAsJsonAsync(requestUri, payload, ct);
        response.EnsureSuccessStatusCode();
    }
}

/// <summary>Request body for interwire connect.</summary>
public sealed record InterwireRequest(
    string SourceNodeId,
    string TargetNodeId,
    int Dimension,
    int SourceX, int SourceY, int SourceZ);

/// <summary>Response body from interwire connect.</summary>
public sealed record InterwireResponse(string ConnectionId);

/// <summary>Request body for interwire disconnect.</summary>
public sealed record DisconnectRequest(string ConnectionId);
