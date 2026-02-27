namespace Ouroboros.Hypergrid.Cli.Api;

using System.Collections.Concurrent;
using Ouroboros.Hypergrid.Host;
using Ouroboros.Hypergrid.Mesh;

/// <summary>
/// Maps the native Hypergrid endpoints: /api/health, /api/status, /api/think, /api/aspect/{id}.
/// </summary>
internal static class HypergridEndpoints
{
    public static void Map(
        IEndpointRouteBuilder app,
        IaretCliHost host,
        OuroborosNode selfNode,
        NodeEndpointRegistry endpointRegistry,
        ConcurrentDictionary<string, StreamConnection> activeConnections)
    {
        var nodeId = selfNode.Id;

        app.MapGet("/api/health", () =>
        {
            var health = selfNode.Health;
            return Results.Ok(new
            {
                nodeId,
                status = health.Status.ToString(),
                position = new { x = selfNode.Position[0], y = selfNode.Position[1], z = selfNode.Position[2] },
                computeBackend = host.Convergence.ComputeBackend,
                environment = host.Environment.Name
            });
        });

        app.MapGet("/api/status", () =>
        {
            var aspects = host.Convergence.Aspects
                .Select(a => new { id = a.Value.AspectId, name = a.Value.Name })
                .ToList();

            return Results.Ok(new
            {
                nodeId,
                computeBackend = host.Convergence.ComputeBackend,
                environment = host.Environment.Name,
                aspects,
                meshPeers = endpointRegistry.RegisteredNodes,
                activeConnections = activeConnections.Count
            });
        });

        app.MapPost("/api/think", async (ThinkRequest request, CancellationToken ct) =>
        {
            var result = await host.ThinkAsync(request.Prompt, ct);
            return Results.Ok(new { response = result });
        });

        app.MapPost("/api/aspect/{aspectId}", async (string aspectId, ThinkRequest request, CancellationToken ct) =>
        {
            var result = await host.AskAspectAsync(aspectId, request.Prompt, ct);
            return Results.Ok(new { aspectId, response = result });
        });
    }
}

/// <summary>Request body for think and aspect endpoints.</summary>
public sealed record ThinkRequest(string Prompt);

/// <summary>Peer endpoint registry for mesh nodes. Parses MESH_PEERS env var.</summary>
public sealed class NodeEndpointRegistry
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Uri> _endpoints = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string nodeId, Uri baseUrl) => _endpoints[nodeId] = baseUrl;

    public IReadOnlyCollection<string> RegisteredNodes => _endpoints.Keys.ToArray();

    public static NodeEndpointRegistry FromEnvironment(string? meshPeers)
    {
        var registry = new NodeEndpointRegistry();
        if (string.IsNullOrWhiteSpace(meshPeers)) return registry;

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
