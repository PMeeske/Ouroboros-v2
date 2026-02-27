<#
.SYNOPSIS
    Ouroboros Hypergrid start script for Windows.

.DESCRIPTION
    Starts the Hypergrid in one of several modes:
      cli    - Interactive console chat with Iaret (default)
      serve  - OpenAI-compatible HTTP API server
      mesh   - Full 3-node Docker Compose mesh with Ollama
      local  - Single local node via dotnet run (Node project)
      build  - Build Docker images only
      stop   - Tear down the Docker Compose mesh
      status - Query health of running nodes

.EXAMPLE
    .\Start-Hypergrid.ps1
    .\Start-Hypergrid.ps1 cli -OllamaUrl http://localhost:11434
    .\Start-Hypergrid.ps1 serve -Port 8080
    .\Start-Hypergrid.ps1 mesh -Detach
    .\Start-Hypergrid.ps1 local
    .\Start-Hypergrid.ps1 stop
    .\Start-Hypergrid.ps1 status
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("cli", "serve", "mesh", "local", "build", "stop", "status", "help")]
    [string]$Command = "cli",

    # --- cloud mode (default: DeepSeek V3 via Ollama Cloud) ---
    [string]$CloudKey,
    [string]$CloudEndpoint,
    [string]$CloudModel,

    # --- local mode ---
    [int]$Port = 9500,
    [string]$NodeId = "local",
    [string]$OllamaUrl,
    [string]$OllamaModel = "deepseek-v3.1:671b-cloud",
    [ValidateSet("cpu", "gpu")]
    [string]$Compute = "cpu",
    [string]$Peers,

    # --- mesh mode ---
    [string]$Model = "iaret"
    [switch]$Detach
)

$ErrorActionPreference = "Stop"

$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ComposeFile  = Join-Path $ScriptDir "docker-compose.mesh.yml"
$NodeProject  = Join-Path $ScriptDir "src\Ouroboros.Hypergrid.Node"
$CliProject   = Join-Path $ScriptDir "src\Ouroboros.Hypergrid.Cli"

# ---- Logging helpers --------------------------------------------------------

function Write-Log    { param([string]$Msg) Write-Host "[hypergrid] $Msg" -ForegroundColor Cyan }
function Write-Ok     { param([string]$Msg) Write-Host "[hypergrid] $Msg" -ForegroundColor Green }
function Write-Warn   { param([string]$Msg) Write-Host "[hypergrid] $Msg" -ForegroundColor Yellow }
function Write-Err    { param([string]$Msg) Write-Host "[hypergrid] $Msg" -ForegroundColor Red }

# ---- Commands ---------------------------------------------------------------

function Invoke-Cli {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Err "dotnet SDK is not installed or not in PATH."
        exit 1
    }

    Write-Log "Starting interactive Hypergrid CLI..."

    $env:COMPUTE_MODE = $Compute
    if ($OllamaUrl)   { $env:OLLAMA_URL   = $OllamaUrl }
    if ($OllamaModel) { $env:OLLAMA_MODEL = $OllamaModel }

    $dotnetArgs = @()
    if ($OllamaUrl) { $dotnetArgs += "--model"; $dotnetArgs += "iaret" }

    dotnet run --project $CliProject -- @dotnetArgs
}

function Invoke-Serve {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Err "dotnet SDK is not installed or not in PATH."
        exit 1
    }

    Write-Log "Starting Hypergrid OpenAI-compatible API on port $Port..."

    $env:COMPUTE_MODE = $Compute
    $env:LISTEN_PORT  = "$Port"
    if ($OllamaUrl)   { $env:OLLAMA_URL   = $OllamaUrl }
    if ($OllamaModel) { $env:OLLAMA_MODEL = $OllamaModel }

    dotnet run --project $CliProject -- --serve --port $Port
}

function Invoke-Mesh {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Err "Docker is not installed or not in PATH."
        exit 1
    }

    Write-Log "Building hypergrid Docker images..."
    docker compose -f $ComposeFile build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Log "Starting 3-node Iaret mesh + Ollama..."
    if ($Detach) {
        docker compose -f $ComposeFile up -d
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        Write-Ok "Mesh started in background."
        Write-Log "Pulling model '$Model' into Ollama..."
        docker compose -f $ComposeFile exec ollama ollama pull $Model
        if ($LASTEXITCODE -ne 0) {
            Write-Warn "Model pull failed - Ollama may still be starting."
            Write-Warn "Retry: docker compose -f $ComposeFile exec ollama ollama pull $Model"
        }

        Write-Host ""
        Write-Ok "Hypergrid mesh is running:"
        Write-Host "  alpha  -> http://localhost:9500/api/health"
        Write-Host "  beta   -> http://localhost:9501/api/health"
        Write-Host "  gamma  -> http://localhost:9502/api/health"
        Write-Host "  ollama -> http://localhost:11434"
    }
    else {
        docker compose -f $ComposeFile up
    }
}

function Invoke-Local {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Err "dotnet SDK is not installed or not in PATH."
        exit 1
    }

    Write-Log "Starting local hypergrid node '$NodeId' on port $Port..."

    if ($OllamaUrl) {
        Write-Log "Ollama: $OllamaUrl (model: $OllamaModel)"
    }
    else {
        Write-Warn "No -OllamaUrl specified - running in local heuristic mode (no LLM)."
    }

    $env:NODE_ID        = $NodeId
    $env:NODE_X         = "0"
    $env:NODE_Y         = "0"
    $env:NODE_Z         = "0"
    $env:COMPUTE_MODE   = $Compute
    $env:ASPNETCORE_URLS = "http://+:$Port"

    if ($OllamaUrl)   { $env:OLLAMA_URL   = $OllamaUrl }
    if ($OllamaModel) { $env:OLLAMA_MODEL = $OllamaModel }
    if ($Peers)       { $env:MESH_PEERS   = $Peers }

    Write-Ok "Endpoints:"
    Write-Host "  health -> http://localhost:$Port/api/health"
    Write-Host "  status -> http://localhost:$Port/api/status"
    Write-Host "  think  -> POST http://localhost:$Port/api/think"
    Write-Host ""

    dotnet run --project $NodeProject
}

function Invoke-Build {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Err "Docker is not installed or not in PATH."
        exit 1
    }

    Write-Log "Building hypergrid Docker images..."
    docker compose -f $ComposeFile build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Ok "Build complete."
}

function Invoke-Stop {
    Write-Log "Stopping hypergrid mesh..."
    docker compose -f $ComposeFile down
    Write-Ok "Mesh stopped."
}

function Invoke-Status {
    $nodes = @(
        @{ Name = "alpha"; Url = "http://localhost:9500/api/health" }
        @{ Name = "beta";  Url = "http://localhost:9501/api/health" }
        @{ Name = "gamma"; Url = "http://localhost:9502/api/health" }
    )

    Write-Host ""
    Write-Log "Checking hypergrid node health..."
    Write-Host ""

    foreach ($node in $nodes) {
        try {
            $response = Invoke-RestMethod -Uri $node.Url -TimeoutSec 3 -ErrorAction Stop
            $json = $response | ConvertTo-Json -Compress
            Write-Ok "  $($node.Name)  [OK]  $json"
        }
        catch {
            Write-Err "  $($node.Name)  [--]  unreachable ($($node.Url))"
        }
    }
    Write-Host ""
}

function Show-Usage {
    Write-Host @"

  Ouroboros Hypergrid Start Script

  Usage: .\Start-Hypergrid.ps1 [COMMAND] [OPTIONS]

  Commands:
    cli      Interactive console chat with Iaret (default)
    serve    Start OpenAI-compatible HTTP API server
    mesh     Start the full 3-node Docker mesh with Ollama
    local    Start a single local node via dotnet run (Node project)
    build    Build Docker images without starting
    stop     Tear down the Docker mesh
    status   Query health of running nodes
    help     Show this message

  Local mode options:
    -Port 9500               HTTP port
    -NodeId "local"          Node identifier
    -OllamaUrl <url>         Ollama endpoint
    -OllamaModel "llama3"    Ollama model name
    -Compute cpu|gpu         Compute backend
    -Peers <urls>            Comma-separated peer URLs

  Mesh mode options:
    -Model "llama3"          Ollama model to pull
    -Detach                  Run containers in background

"@
}

# ---- Main -------------------------------------------------------------------

switch ($Command) {
    "cli"    { Invoke-Cli }
    "serve"  { Invoke-Serve }
    "mesh"   { Invoke-Mesh }
    "local"  { Invoke-Local }
    "build"  { Invoke-Build }
    "stop"   { Invoke-Stop }
    "status" { Invoke-Status }
    "help"   { Show-Usage }
}
