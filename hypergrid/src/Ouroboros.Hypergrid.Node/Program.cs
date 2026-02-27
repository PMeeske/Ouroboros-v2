using System.Collections.Concurrent;
using Ouroboros.Hypergrid.Host;
using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Mesh;
using Ouroboros.Hypergrid.Node;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Topology;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration from environment variables ---
var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? Environment.MachineName;
var nodeX = int.TryParse(Environment.GetEnvironmentVariable("NODE_X"), out var x) ? x : 0;
var nodeY = int.TryParse(Environment.GetEnvironmentVariable("NODE_Y"), out var y) ? y : 0;
var nodeZ = int.TryParse(Environment.GetEnvironmentVariable("NODE_Z"), out var z) ? z : 0;
var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_URL");
var meshPeers = Environment.GetEnvironmentVariable("MESH_PEERS");
var computeMode = Environment.GetEnvironmentVariable("COMPUTE_MODE")?.ToLowerInvariant();

// --- Build simulator ---
IGridSimulator simulator = computeMode switch
{
    "cpu" => SimulatorFactory.CreateCpu(),
    _ => SimulatorFactory.Create()
};

// --- Build Iaret host ---
IaretCliHost host;
if (!string.IsNullOrWhiteSpace(ollamaUrl))
{
    // Create an HTTP-based pipeline pointing at Ollama
    var pipeline = new OllamaHttpPipeline(new Uri(ollamaUrl));
    host = IaretCliHost.Create(pipeline, simulator);
}
else
{
    // No LLM available — local heuristic mode
    host = IaretCliHost.CreateLocal(simulator);
}

// --- Mesh setup ---
var registry = NodeEndpointRegistry.FromEnvironment(meshPeers);
var httpClient = new HttpClient();
var interwire = new HttpInterwire(httpClient, registry);
var grid = new HypergridSpace(new[]
{
    new DimensionDescriptor(0, "Temporal", "Time/sequence axis"),
    new DimensionDescriptor(1, "Semantic", "Meaning/concept axis"),
    new DimensionDescriptor(2, "Causal", "Cause-effect axis"),
});
var mesh = new MeshOrchestrator(grid, interwire);
var selfNode = mesh.Register(nodeId, new GridCoordinate(nodeX, nodeY, nodeZ));
selfNode.ReportHealthy();

// Track active inbound connections
var activeConnections = new ConcurrentDictionary<string, StreamConnection>();

var app = builder.Build();

// === API Endpoints ===

app.MapGet("/api/health", () =>
{
    var health = selfNode.Health;
    return Results.Ok(new
    {
        nodeId,
        status = health.Status.ToString(),
        position = new { x = nodeX, y = nodeY, z = nodeZ },
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
        meshPeers = registry.RegisteredNodes,
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

app.MapPost("/api/interwire/connect", (InterwireRequest request) =>
{
    var connectionId = Guid.NewGuid().ToString("N");
    var sourceNode = new OuroborosNode(
        request.SourceNodeId,
        new GridCoordinate(request.SourceX, request.SourceY, request.SourceZ));
    var edge = new GridEdge(
        sourceNode.Position,
        selfNode.Position,
        request.Dimension);
    var conn = new StreamConnection(connectionId, sourceNode, selfNode, edge);
    activeConnections[connectionId] = conn;
    return Results.Ok(new InterwireResponse(connectionId));
});

app.MapPost("/api/interwire/disconnect", (DisconnectRequest request) =>
{
    activeConnections.TryRemove(request.ConnectionId, out _);
    return Results.Ok();
});

app.Run();

// --- Supporting types ---

/// <summary>Request body for think and aspect endpoints.</summary>
public sealed record ThinkRequest(string Prompt);

/// <summary>
/// Minimal HTTP-based pipeline that forwards prompts to an Ollama instance.
/// This is the simplest possible bridge — one-shot text generation only.
/// </summary>
internal sealed class OllamaHttpPipeline : IOuroborosPipeline
{
    private readonly HttpClient _http = new();
    private readonly Uri _baseUrl;

    public OllamaHttpPipeline(Uri baseUrl) => _baseUrl = baseUrl;

    public string ModelName => $"Ollama@{_baseUrl.Host}";
    public bool SupportsStreaming => false;

    public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
    {
        var body = new
        {
            model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "deepseek-v3.1:671b-cloud"
            prompt = systemPrompt is not null ? $"[System: {systemPrompt}]\n{prompt}" : prompt,
            stream = false
        };

        var response = await _http.PostAsJsonAsync(new Uri(_baseUrl, "/api/generate"), body, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);
        return result?.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string? systemPrompt = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return await GenerateAsync(prompt, systemPrompt, ct);
    }

    private sealed record OllamaResponse(string Response);
}
