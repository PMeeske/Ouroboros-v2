#!/usr/bin/env bash
set -euo pipefail

# ============================================================================
#  Ouroboros Hypergrid — Start Script
#
#  Modes:
#    cli     — Interactive console chat with Iaret           (default)
#    serve   — OpenAI-compatible HTTP API server
#    mesh    — Full 3-node Docker Compose mesh with Ollama
#    local   — Single local node via dotnet run (Node project)
#    build   — Build Docker images only (no start)
#    stop    — Tear down the Docker Compose mesh
#    status  — Show running node health
# ============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.mesh.yml"
NODE_PROJECT="$SCRIPT_DIR/src/Ouroboros.Hypergrid.Node"
CLI_PROJECT="$SCRIPT_DIR/src/Ouroboros.Hypergrid.Cli"

# Defaults for local mode
LOCAL_PORT="${HYPERGRID_PORT:-9500}"
LOCAL_NODE_ID="${NODE_ID:-local}"
LOCAL_COMPUTE="${COMPUTE_MODE:-cpu}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m'

usage() {
    cat <<EOF
Usage: $(basename "$0") [COMMAND] [OPTIONS]

Commands:
  cli      Interactive console chat with Iaret (default)
  serve    Start OpenAI-compatible HTTP API server
  mesh     Start the full 3-node Docker mesh with Ollama
  local    Start a single local node via dotnet run (Node project)
  build    Build Docker images without starting
  stop     Tear down the Docker mesh
  status   Query health of running nodes

Options (cli/serve mode):
  --cloud-key KEY        Ollama Cloud API key (env: OLLAMA_CLOUD_API_KEY)
  --cloud-endpoint URL   Ollama Cloud endpoint (default: https://api.ollama.ai)
  --cloud-model MODEL    Cloud model (default: deepseek-v3.1:671b-cloud)
  --ollama-url URL       Local Ollama endpoint (env: OLLAMA_URL)
  --ollama-model MODEL   Ollama model name (default: llama3, env: OLLAMA_MODEL)
  --compute MODE         cpu | gpu (default: cpu, env: COMPUTE_MODE)

Options (local mode):
  --port PORT          HTTP port (default: 9500, env: HYPERGRID_PORT)
  --node-id ID         Node identifier (default: local, env: NODE_ID)
  --peers PEERS        Comma-separated peer URLs (env: MESH_PEERS)

Options (mesh mode):
  --model MODEL        Ollama model to pull before starting (default: llama3)
  --detach             Run containers in background

Examples:
  $(basename "$0")                              # Interactive chat (cloud if key set)
  $(basename "$0") cli --cloud-key sk-xxx       # Iaret via Ollama Cloud DeepSeek
  $(basename "$0") cli --ollama-url http://localhost:11434
  $(basename "$0") serve --port 8080            # OpenAI-compatible API
  $(basename "$0") mesh --detach                # Docker mesh in background
  $(basename "$0") local                        # Single local node
  $(basename "$0") stop                         # Tear down mesh
  $(basename "$0") status                       # Check node health
EOF
    exit 0
}

log()  { echo -e "${CYAN}[hypergrid]${NC} $*"; }
ok()   { echo -e "${GREEN}[hypergrid]${NC} $*"; }
warn() { echo -e "${YELLOW}[hypergrid]${NC} $*"; }
err()  { echo -e "${RED}[hypergrid]${NC} $*" >&2; }

# ---- Commands ---------------------------------------------------------------

cmd_cli() {
    local ollama_url="${OLLAMA_URL:-}"
    local ollama_model="${OLLAMA_MODEL:-llama3}"
    local cloud_key="${OLLAMA_CLOUD_API_KEY:-}"
    local cloud_endpoint="${OLLAMA_CLOUD_ENDPOINT:-}"
    local cloud_model="${OLLAMA_CLOUD_MODEL:-}"

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --ollama-url)      ollama_url="$2"; shift 2 ;;
            --ollama-model)    ollama_model="$2"; shift 2 ;;
            --cloud-key)       cloud_key="$2"; shift 2 ;;
            --cloud-endpoint)  cloud_endpoint="$2"; shift 2 ;;
            --cloud-model)     cloud_model="$2"; shift 2 ;;
            --compute)         LOCAL_COMPUTE="$2"; shift 2 ;;
            *)                 err "Unknown option: $1"; usage ;;
        esac
    done

    if ! command -v dotnet &>/dev/null; then
        err "dotnet SDK is not installed or not in PATH."
        exit 1
    fi

    log "Starting interactive Hypergrid CLI..."
    export COMPUTE_MODE="$LOCAL_COMPUTE"
    [[ -n "$ollama_url" ]]      && export OLLAMA_URL="$ollama_url"
    [[ -n "$ollama_model" ]]    && export OLLAMA_MODEL="$ollama_model"
    [[ -n "$cloud_key" ]]       && export OLLAMA_CLOUD_API_KEY="$cloud_key"
    [[ -n "$cloud_endpoint" ]]  && export OLLAMA_CLOUD_ENDPOINT="$cloud_endpoint"
    [[ -n "$cloud_model" ]]     && export OLLAMA_CLOUD_MODEL="$cloud_model"

    if [[ -n "$cloud_key" ]]; then
        ok "Using Ollama Cloud (${cloud_model:-deepseek-v3.1:671b-cloud})"
    elif [[ -n "$ollama_url" ]]; then
        ok "Using local Ollama ($ollama_url, model: $ollama_model)"
    else
        warn "No LLM configured — running in local heuristic mode."
    fi

    dotnet run --project "$CLI_PROJECT"
}

cmd_serve() {
    local port="${LISTEN_PORT:-8080}"
    local ollama_url="${OLLAMA_URL:-}"
    local ollama_model="${OLLAMA_MODEL:-llama3}"
    local cloud_key="${OLLAMA_CLOUD_API_KEY:-}"
    local cloud_endpoint="${OLLAMA_CLOUD_ENDPOINT:-}"
    local cloud_model="${OLLAMA_CLOUD_MODEL:-}"

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --port)            port="$2"; shift 2 ;;
            --ollama-url)      ollama_url="$2"; shift 2 ;;
            --ollama-model)    ollama_model="$2"; shift 2 ;;
            --cloud-key)       cloud_key="$2"; shift 2 ;;
            --cloud-endpoint)  cloud_endpoint="$2"; shift 2 ;;
            --cloud-model)     cloud_model="$2"; shift 2 ;;
            --compute)         LOCAL_COMPUTE="$2"; shift 2 ;;
            *)                 err "Unknown option: $1"; usage ;;
        esac
    done

    if ! command -v dotnet &>/dev/null; then
        err "dotnet SDK is not installed or not in PATH."
        exit 1
    fi

    log "Starting Hypergrid OpenAI-compatible API on port $port..."
    export COMPUTE_MODE="$LOCAL_COMPUTE"
    export LISTEN_PORT="$port"
    [[ -n "$ollama_url" ]]      && export OLLAMA_URL="$ollama_url"
    [[ -n "$ollama_model" ]]    && export OLLAMA_MODEL="$ollama_model"
    [[ -n "$cloud_key" ]]       && export OLLAMA_CLOUD_API_KEY="$cloud_key"
    [[ -n "$cloud_endpoint" ]]  && export OLLAMA_CLOUD_ENDPOINT="$cloud_endpoint"
    [[ -n "$cloud_model" ]]     && export OLLAMA_CLOUD_MODEL="$cloud_model"

    dotnet run --project "$CLI_PROJECT" -- --serve --port "$port"
}

cmd_mesh() {
    local detach=""
    local model="llama3"

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --detach|-d) detach="-d"; shift ;;
            --model)     model="$2"; shift 2 ;;
            *)           err "Unknown option: $1"; usage ;;
        esac
    done

    if ! command -v docker &>/dev/null; then
        err "Docker is not installed or not in PATH."
        exit 1
    fi

    log "Building hypergrid Docker images..."
    docker compose -f "$COMPOSE_FILE" build

    log "Starting 3-node Iaret mesh + Ollama..."
    docker compose -f "$COMPOSE_FILE" up $detach

    if [[ -n "$detach" ]]; then
        ok "Mesh started in background."
        log "Pulling model '$model' into Ollama..."
        docker compose -f "$COMPOSE_FILE" exec ollama ollama pull "$model" || warn "Model pull failed — Ollama may still be starting. Retry with: docker compose -f $COMPOSE_FILE exec ollama ollama pull $model"
        echo ""
        ok "Hypergrid mesh is running:"
        echo "  alpha  → http://localhost:9500/api/health"
        echo "  beta   → http://localhost:9501/api/health"
        echo "  gamma  → http://localhost:9502/api/health"
        echo "  ollama → http://localhost:11434"
    fi
}

cmd_local() {
    local ollama_url="${OLLAMA_URL:-}"
    local ollama_model="${OLLAMA_MODEL:-llama3}"
    local peers="${MESH_PEERS:-}"

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --port)          LOCAL_PORT="$2"; shift 2 ;;
            --node-id)       LOCAL_NODE_ID="$2"; shift 2 ;;
            --ollama-url)    ollama_url="$2"; shift 2 ;;
            --ollama-model)  ollama_model="$2"; shift 2 ;;
            --compute)       LOCAL_COMPUTE="$2"; shift 2 ;;
            --peers)         peers="$2"; shift 2 ;;
            *)               err "Unknown option: $1"; usage ;;
        esac
    done

    if ! command -v dotnet &>/dev/null; then
        err "dotnet SDK is not installed or not in PATH."
        exit 1
    fi

    log "Starting local hypergrid node '$LOCAL_NODE_ID' on port $LOCAL_PORT..."
    [[ -n "$ollama_url" ]] && log "Ollama: $ollama_url (model: $ollama_model)" || warn "No OLLAMA_URL — running in local heuristic mode (no LLM)."

    export NODE_ID="$LOCAL_NODE_ID"
    export NODE_X=0
    export NODE_Y=0
    export NODE_Z=0
    export COMPUTE_MODE="$LOCAL_COMPUTE"
    export ASPNETCORE_URLS="http://+:$LOCAL_PORT"

    [[ -n "$ollama_url" ]]   && export OLLAMA_URL="$ollama_url"
    [[ -n "$ollama_model" ]] && export OLLAMA_MODEL="$ollama_model"
    [[ -n "$peers" ]]        && export MESH_PEERS="$peers"

    ok "Endpoints:"
    echo "  health → http://localhost:$LOCAL_PORT/api/health"
    echo "  status → http://localhost:$LOCAL_PORT/api/status"
    echo "  think  → POST http://localhost:$LOCAL_PORT/api/think"
    echo ""

    dotnet run --project "$NODE_PROJECT"
}

cmd_build() {
    if ! command -v docker &>/dev/null; then
        err "Docker is not installed or not in PATH."
        exit 1
    fi

    log "Building hypergrid Docker images..."
    docker compose -f "$COMPOSE_FILE" build
    ok "Build complete."
}

cmd_stop() {
    log "Stopping hypergrid mesh..."
    docker compose -f "$COMPOSE_FILE" down
    ok "Mesh stopped."
}

cmd_status() {
    local nodes=("http://localhost:9500" "http://localhost:9501" "http://localhost:9502")
    local names=("alpha" "beta" "gamma")

    echo ""
    log "Checking hypergrid node health..."
    echo ""

    for i in "${!nodes[@]}"; do
        local url="${nodes[$i]}/api/health"
        local name="${names[$i]}"
        if response=$(curl -sf --max-time 3 "$url" 2>/dev/null); then
            ok "  $name  ✓  $response"
        else
            err "  $name  ✗  unreachable ($url)"
        fi
    done
    echo ""
}

# ---- Main -------------------------------------------------------------------

COMMAND="${1:-cli}"
shift 2>/dev/null || true

case "$COMMAND" in
    cli)     cmd_cli "$@" ;;
    serve)   cmd_serve "$@" ;;
    mesh)    cmd_mesh "$@" ;;
    local)   cmd_local "$@" ;;
    build)   cmd_build "$@" ;;
    stop)    cmd_stop "$@" ;;
    status)  cmd_status "$@" ;;
    -h|--help|help) usage ;;
    *)       err "Unknown command: $COMMAND"; usage ;;
esac
