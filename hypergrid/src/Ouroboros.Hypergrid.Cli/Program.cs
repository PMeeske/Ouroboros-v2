using System.Collections.Concurrent;
using Ouroboros.Hypergrid.Cli.Api;
using Ouroboros.Hypergrid.Cli.Providers;
using Ouroboros.Hypergrid.Host;
using Ouroboros.Hypergrid.Mesh;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Topology;

// ============================================================================
//  Ouroboros Hypergrid CLI
//
//  Default:   Interactive console chat with Iaret
//  --serve:   Start OpenAI-compatible HTTP API server
//  --model:   Select initial model (default: iaret)
//  --port:    HTTP port for --serve mode (default: 8080)
// ============================================================================

// --- Parse arguments --------------------------------------------------------

var serve = args.Contains("--serve");
var portArg = GetArgValue(args, "--port");
var modelArg = GetArgValue(args, "--model");
var listenPort = int.TryParse(portArg ?? Environment.GetEnvironmentVariable("LISTEN_PORT"), out var p) ? p : 8080;

// --- Configuration from environment variables (same pattern as Node) --------

var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? Environment.MachineName;
var nodeX = int.TryParse(Environment.GetEnvironmentVariable("NODE_X"), out var x) ? x : 0;
var nodeY = int.TryParse(Environment.GetEnvironmentVariable("NODE_Y"), out var y) ? y : 0;
var nodeZ = int.TryParse(Environment.GetEnvironmentVariable("NODE_Z"), out var z) ? z : 0;
var meshPeers = Environment.GetEnvironmentVariable("MESH_PEERS");
var computeMode = Environment.GetEnvironmentVariable("COMPUTE_MODE")?.ToLowerInvariant();
var upstreamProviders = Environment.GetEnvironmentVariable("UPSTREAM_PROVIDERS");

// Ollama local
var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_URL");
var defaultOllamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3";

// Ollama Cloud (default: DeepSeek V3 via Ollama Cloud)
var ollamaCloudKey = Environment.GetEnvironmentVariable("OLLAMA_CLOUD_API_KEY");
var ollamaCloudEndpoint = Environment.GetEnvironmentVariable("OLLAMA_CLOUD_ENDPOINT") ?? "https://api.ollama.ai";
var ollamaCloudModel = Environment.GetEnvironmentVariable("OLLAMA_CLOUD_MODEL") ?? "deepseek-v3.1:671b-cloud";

// --- Build simulator --------------------------------------------------------

IGridSimulator simulator = computeMode switch
{
    "cpu" => SimulatorFactory.CreateCpu(),
    _ => SimulatorFactory.Create()
};

// --- Build Iaret host -------------------------------------------------------
//
//  Priority:
//    1. Ollama Cloud (OLLAMA_CLOUD_API_KEY) — DeepSeek V3 via Ollama Cloud (default)
//    2. Local Ollama  (OLLAMA_URL)          — any local model
//    3. Local heuristic                     — no LLM, convergence transforms only
//

IaretCliHost host;
OllamaHttpPipeline? cloudPipeline = null;
OllamaHttpPipeline? ollamaPipeline = null;

if (!string.IsNullOrWhiteSpace(ollamaCloudKey))
{
    cloudPipeline = new OllamaHttpPipeline(new Uri(ollamaCloudEndpoint), ollamaCloudModel, ollamaCloudKey);
    host = IaretCliHost.Create(cloudPipeline, simulator);
}
else if (!string.IsNullOrWhiteSpace(ollamaUrl))
{
    ollamaPipeline = new OllamaHttpPipeline(new Uri(ollamaUrl), defaultOllamaModel);
    host = IaretCliHost.Create(ollamaPipeline, simulator);
}
else
{
    host = IaretCliHost.CreateLocal(simulator);
}

// --- Build provider registry ------------------------------------------------

var registry = new PipelineProviderRegistry();
registry.Register("iaret", host.Pipeline);

if (cloudPipeline is not null)
    registry.Register("cloud", cloudPipeline);

if (ollamaPipeline is not null)
    registry.Register("ollama", ollamaPipeline);

// If both cloud and local Ollama configured, also register local Ollama
if (cloudPipeline is not null && !string.IsNullOrWhiteSpace(ollamaUrl))
{
    ollamaPipeline = new OllamaHttpPipeline(new Uri(ollamaUrl), defaultOllamaModel);
    registry.Register("ollama", ollamaPipeline);
}

if (!string.IsNullOrWhiteSpace(upstreamProviders))
{
    foreach (var entry in upstreamProviders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var parts = entry.Split('=', 2);
        if (parts.Length == 2)
        {
            var name = parts[0];
            var urlKey = parts[1].Split(':', 2);
            registry.Register(name, new UpstreamHttpPipeline(name, new Uri(urlKey[0]), urlKey.Length > 1 ? urlKey[1] : null));
        }
    }
}

// --- Mesh setup -------------------------------------------------------------

var endpointRegistry = NodeEndpointRegistry.FromEnvironment(meshPeers);
var grid = new HypergridSpace(new[]
{
    new DimensionDescriptor(0, "Temporal", "Time/sequence axis"),
    new DimensionDescriptor(1, "Semantic", "Meaning/concept axis"),
    new DimensionDescriptor(2, "Causal", "Cause-effect axis"),
});
var mesh = new MeshOrchestrator(grid, new NullInterwire());
var selfNode = mesh.Register(nodeId, new GridCoordinate(nodeX, nodeY, nodeZ));
selfNode.ReportHealthy();
var activeConnections = new ConcurrentDictionary<string, StreamConnection>();

// --- Mode dispatch ----------------------------------------------------------

if (serve)
{
    await RunServer();
}
else
{
    await RunInteractiveChat();
}

// ============================================================================
//  Interactive Console Chat
// ============================================================================

async Task RunInteractiveChat()
{
    var currentModel = modelArg ?? "iaret";

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  Ouroboros Hypergrid CLI");
    Console.WriteLine("  ----------------------");
    Console.ResetColor();
    Console.WriteLine($"  Backend : {host.Convergence.ComputeBackend}");
    Console.WriteLine($"  Env     : {host.Environment.Name}");
    Console.WriteLine($"  LLM     : {(cloudPipeline is not null ? $"Ollama Cloud ({ollamaCloudModel})" : ollamaPipeline is not null ? $"Ollama Local ({defaultOllamaModel})" : "Heuristic (no LLM)")}");
    Console.WriteLine($"  Model   : {currentModel}");
    Console.Write("  Providers: ");
    Console.WriteLine(string.Join(", ", registry.ListProviders().Select(p => p.Prefix)));
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Commands: /models  /use <model>  /aspect <id> <prompt>  /serve  /quit");
    Console.ResetColor();
    Console.WriteLine();

    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"[{currentModel}] > ");
        Console.ResetColor();

        var input = Console.ReadLine();
        if (input is null) break; // EOF
        input = input.Trim();
        if (string.IsNullOrEmpty(input)) continue;

        // --- Slash commands ---
        if (input.StartsWith('/'))
        {
            var cmd = input.Split(' ', 2, StringSplitOptions.TrimEntries);
            switch (cmd[0].ToLowerInvariant())
            {
                case "/quit" or "/exit" or "/q":
                    return;

                case "/models":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    foreach (var (prefix, modelName) in registry.ListProviders())
                        Console.WriteLine($"  {prefix,-20} ({modelName})");
                    Console.ResetColor();
                    continue;

                case "/use":
                    if (cmd.Length < 2) { Console.WriteLine("  Usage: /use <model>"); continue; }
                    currentModel = cmd[1];
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"  Switched to: {currentModel}");
                    Console.ResetColor();
                    continue;

                case "/aspect":
                    if (cmd.Length < 2) { Console.WriteLine("  Usage: /aspect <id> <prompt>"); continue; }
                    var aspectParts = cmd[1].Split(' ', 2, StringSplitOptions.TrimEntries);
                    if (aspectParts.Length < 2) { Console.WriteLine("  Usage: /aspect <id> <prompt>"); continue; }
                    try
                    {
                        var aspectResult = await host.AskAspectAsync(aspectParts[0], aspectParts[1]);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"  [{aspectParts[0]}] {aspectResult}");
                        Console.ResetColor();
                    }
                    catch (Exception ex) { WriteError(ex.Message); }
                    continue;

                case "/serve":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  Starting HTTP API on port {listenPort}...");
                    Console.ResetColor();
                    await RunServer();
                    return;

                case "/status":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"  Node     : {nodeId}");
                    Console.WriteLine($"  Backend  : {host.Convergence.ComputeBackend}");
                    Console.WriteLine($"  Env      : {host.Environment.Name}");
                    Console.WriteLine($"  LLM      : {(cloudPipeline is not null ? $"Ollama Cloud ({ollamaCloudModel})" : ollamaPipeline is not null ? $"Ollama Local ({defaultOllamaModel})" : "Heuristic")}");
                    Console.WriteLine($"  Aspects  : {string.Join(", ", host.Convergence.Aspects.Keys)}");
                    Console.WriteLine($"  Model    : {currentModel}");
                    Console.ResetColor();
                    continue;

                case "/help":
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  /models              List available providers");
                    Console.WriteLine("  /use <model>         Switch model (e.g., iaret, cloud, ollama)");
                    Console.WriteLine("                       Sub-model: cloud/deepseek-r1:32b, ollama/codestral");
                    Console.WriteLine("  /aspect <id> <prompt> Query a specific Iaret aspect directly");
                    Console.WriteLine("  /status              Show node and convergence status");
                    Console.WriteLine("  /serve               Switch to HTTP API server mode");
                    Console.WriteLine("  /quit                Exit");
                    Console.ResetColor();
                    continue;

                default:
                    Console.WriteLine($"  Unknown command: {cmd[0]}. Type /help for commands.");
                    continue;
            }
        }

        // --- Generate response via current model ---
        var resolved = registry.Resolve(currentModel);
        if (resolved is null)
        {
            WriteError($"Model '{currentModel}' not found. Use /models to list available providers.");
            continue;
        }

        var (pipeline, effectiveModel) = resolved.Value;
        // Only override the model on passthrough pipelines when they're the direct target.
        // When "iaret" is selected, the convergence cycle calls Ollama internally —
        // it must use Ollama's configured default model, not "iaret" (which Ollama doesn't have).
        OllamaHttpPipeline.SetRequestModel(pipeline is OllamaHttpPipeline ? effectiveModel : null);
        UpstreamHttpPipeline.SetRequestModel(pipeline is UpstreamHttpPipeline ? effectiveModel : null);

        try
        {
            Console.ForegroundColor = ConsoleColor.White;
            if (pipeline.SupportsStreaming)
            {
                await foreach (var token in pipeline.StreamAsync(input, ct: CancellationToken.None))
                {
                    Console.Write(token);
                }
                Console.WriteLine();
            }
            else
            {
                var result = await pipeline.GenerateAsync(input);
                Console.WriteLine(result);
            }
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            WriteError(ex.Message);
        }

        Console.WriteLine();
    }
}

// ============================================================================
//  HTTP API Server (OpenAI-compatible)
// ============================================================================

async Task RunServer()
{
    var builder = WebApplication.CreateBuilder(args.Where(a => !a.StartsWith("--serve")).ToArray());
    var app = builder.Build();

    // OpenAI-compatible endpoints
    ChatCompletionEndpoints.Map(app, registry);
    ModelEndpoints.Map(app, registry);

    // Native Hypergrid endpoints
    HypergridEndpoints.Map(app, host, selfNode, endpointRegistry, activeConnections);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  Ouroboros Hypergrid API Server");
    Console.WriteLine("  -----------------------------");
    Console.ResetColor();
    Console.WriteLine($"  Port      : {listenPort}");
    Console.WriteLine($"  Backend   : {host.Convergence.ComputeBackend}");
    Console.WriteLine($"  Env       : {host.Environment.Name}");
    Console.Write("  Providers : ");
    Console.WriteLine(string.Join(", ", registry.ListProviders().Select(p => p.Prefix)));
    Console.WriteLine();
    Console.WriteLine("  Endpoints:");
    Console.WriteLine($"    POST http://localhost:{listenPort}/v1/chat/completions");
    Console.WriteLine($"    GET  http://localhost:{listenPort}/v1/models");
    Console.WriteLine($"    GET  http://localhost:{listenPort}/api/health");
    Console.WriteLine($"    POST http://localhost:{listenPort}/api/think");
    Console.WriteLine();

    app.Urls.Add($"http://0.0.0.0:{listenPort}");
    await app.RunAsync();
}

// ============================================================================
//  Helpers
// ============================================================================

static string? GetArgValue(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Error: {message}");
    Console.ResetColor();
}

/// <summary>
/// No-op interwire for standalone CLI mode (no mesh networking needed).
/// </summary>
internal sealed class NullInterwire : IInterwire
{
    public Task<StreamConnection> Connect(OuroborosNode source, OuroborosNode target, GridEdge edge, CancellationToken ct)
        => Task.FromResult(new StreamConnection(Guid.NewGuid().ToString("N"), source, target, edge));

    public Task Disconnect(StreamConnection connection, CancellationToken ct)
        => Task.CompletedTask;
}
