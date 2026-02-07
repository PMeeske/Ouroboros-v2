<p align="center">
  <img src="assets/ouroboros-icon.svg" alt="Ouroboros Logo" width="180" height="180">
</p>

<h1 align="center">Ouroboros</h1>

<p align="center">
  <img src="https://img.shields.io/badge/tests-2549%20passing%2C%2015%20failing-red" alt="Tests">
  <img src="https://img.shields.io/badge/coverage-8.7%25-red" alt="Coverage">
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-blue" alt=".NET Version"></a>
  <a href="https://www.nuget.org/packages/LangChain/"><img src="https://img.shields.io/badge/LangChain-0.17.0-purple" alt="LangChain"></a>
</p>

A **sophisticated functional programming-based AI pipeline system** (YET EXPERIMENTAL) built on LangChain, implementing category theory principles, monadic composition, and functional programming patterns to create robust, self-improving AI agents.

## Repository Structure

This repository uses **git submodules** to compose the full system from independent sub-repos:

| Submodule | Repository | Description |
|-----------|-----------|-------------|
| `.build/` | [ouroboros-build](https://github.com/PMeeske/ouroboros-build) | Shared build config, CI templates, TestKit |
| `foundation/` | [ouroboros-foundation](https://github.com/PMeeske/ouroboros-foundation) | Core, Domain, Tools, Genetic, Roslynator |
| `engine/` | [ouroboros-engine](https://github.com/PMeeske/ouroboros-engine) | Agent, Pipeline, Providers, Network |
| `app/` | [ouroboros-app](https://github.com/PMeeske/ouroboros-app) | Application, CLI, WebApi, Android, Easy |

### Build Inheritance

```
ouroboros-build (Directory.Build.props)         ← Base: analyzers, lang version, warnings
    │
    ├── ouroboros-foundation                    ← Layer: Foundation (no upstream deps)
    │
    ├── ouroboros-engine                        ← Layer: Engine (depends on Foundation)
    │
    └── ouroboros-app                           ← Layer: App (depends on Foundation + Engine)
```

## Key Features

- **Monadic Composition**: Type-safe pipeline operations using `Result<T>` and `Option<T>` monads
- **Kleisli Arrows**: Mathematical composition of computations in monadic contexts
- **LangChain Integration**: Native integration with LangChain providers and tools
- **LangChain Pipe Operators**: Familiar `Set | Retrieve | Template | LLM` syntax with monadic safety
- **Meta-AI Layer**: Pipeline steps exposed as tools - the LLM can invoke pipeline operations
- **AI Orchestrator**: Performance-aware model selection based on use case classification
- **Meta-AI Layer v2**: Planner/Executor/Verifier orchestrator with continual learning
- **Self-Improving Agents**: Automatic skill extraction and learning from successful executions
- **Enhanced Memory**: Persistent memory with consolidation and intelligent forgetting
- **Uncertainty Routing**: Confidence-aware task routing with fallback strategies
- **Phase 2 Metacognition**: Agent self-model, goal hierarchy, and autonomous self-evaluation
  - **Capability Registry**: Agent understands its own capabilities and limitations
  - **Goal Hierarchy**: Hierarchical goal decomposition with value alignment
  - **Self-Evaluator**: Autonomous performance assessment and improvement planning
- **Phase 2 — Integrated Self-Model**: Persistent self-model with global workspace and predictive monitoring
  - **Identity Graph**: Tracks capabilities, resources, commitments, and performance metrics
  - **Global Workspace**: Shared working memory with attention-based priority management
  - **Cognitive Processing**: Global Workspace Theory integration with Pavlovian consciousness ([docs](engine/docs/COGNITIVE_ARCHITECTURE.md))
  - **Predictive Monitoring**: Forecast tracking, calibration metrics, and anomaly detection
  - **Self-Explanation**: Generate narratives from execution DAG for transparency
  - **API Endpoints**: `/api/self/state`, `/api/self/forecast`, `/api/self/commitments`, `/api/self/explain`
  - **CLI Commands**: `self state`, `self forecast`, `self commitments`, `self explain`
- **Epic Branch Orchestration**: Automated epic management with agent assignment and dedicated branches
  - **Auto Agent Assignment**: Each sub-issue gets its own dedicated agent
  - **Dedicated Branches**: Isolated work tracking with immutable pipeline branches
  - **Parallel Execution**: Concurrent sub-issue processing with Result monads
- **GitHub Copilot Development Loop**: Automated development workflows powered by AI
  - **Automated Code Review**: AI-assisted PR reviews with functional programming pattern checks
  - **Issue Analysis**: Automatic issue classification and implementation guidance
  - **Continuous Improvement**: Weekly code quality analysis and optimization suggestions
- **Phase 0 — Evolution Foundations**: Infrastructure for evolutionary metacognitive control
  - **Feature Flags**: Modular enablement of `embodiment`, `self_model`, and `affect` capabilities
  - **DAG Maintenance**: Snapshot integrity with SHA-256 hashing and retention policies
  - **Global Projection Service**: System-wide state observation with epoch snapshots and metrics
  - **CLI Commands**: `dag snapshot`, `dag show`, `dag replay`, `dag validate`, `dag retention`
- **Convenience Layer**: Simplified one-liner methods for quick orchestrator setup
- **MeTTa Symbolic Reasoning**: Hybrid neural-symbolic AI with MeTTa integration
- **Vector Database Support**: Built-in vector storage and retrieval capabilities
- **Event Sourcing**: Complete audit trail with replay functionality
- **Extensible Tool System**: Plugin architecture for custom tools and functions
- **Memory Management**: Multiple conversation memory strategies
- **RecursiveChunkProcessor**: Process large contexts (100+ pages) with adaptive chunking and map-reduce
- **Type Safety**: Leverages C# type system for compile-time guarantees
- **IONOS Cloud Ready**: Optimized deployment for IONOS Cloud Kubernetes infrastructure
- **Multi-Provider Support**: Native support for Anthropic Claude, OpenAI, Ollama, GitHub Models, and more
- **Cost Tracking**: Built-in LLM usage cost tracking with session summaries

## Architecture

Ouroboros follows a **Functional Pipeline Architecture** with monadic composition as its central organizing principle:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Core Layer    │    │  Domain Layer   │    │ Pipeline Layer  │
│                 │    │                 │    │                 │
│ • Monads        │───▶│ • Events        │───▶│ • Branches      │
│ • Kleisli       │    │ • States        │    │ • Vectors       │
│ • Steps         │    │ • Vectors       │    │ • Ingestion     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 ▼
                    ┌─────────────────┐
                    │Integration Layer│
                    │                 │
                    │ • Tools         │
                    │ • Providers     │
                    │ • Memory        │
                    └─────────────────┘
```

See **[Detailed Architectural Layer Diagram](engine/docs/ARCHITECTURAL_LAYERS.md)** for comprehensive system architecture documentation.

### Iterative Refinement Architecture

The reasoning pipeline implements a **sophisticated iterative refinement architecture** that enables true progressive enhancement across multiple critique-improve cycles:

```
Iteration 0:  Draft ──────────────────────────────────────┐
                │                                          │
Iteration 1:  Critique(Draft) → Improve → FinalSpec₁     │
                                              │            │
Iteration 2:  Critique(FinalSpec₁) → Improve → FinalSpec₂│
                                              │            │
Iteration N:  Critique(FinalSpec_{N-1}) → Improve → FinalSpec_N
```

**Key Architectural Features:**
- **State Chaining**: Each iteration uses `GetMostRecentReasoningState()` to build upon the previous improvement
- **Polymorphic States**: Both `Draft` and `FinalSpec` are `ReasoningState` instances that can be processed uniformly
- **Event Sourcing**: Complete immutable audit trail enables replay and analysis of the entire reasoning process
- **Monadic Composition**: `CritiqueArrow` and `ImproveArrow` compose as pure Kleisli arrows for type-safe pipelines

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Ollama](https://ollama.ai/) (for local LLM providers) or remote API access (optional)

### Installation

1. **Clone the repository:**
   ```bash
   git clone --recurse-submodules https://github.com/PMeeske/Ouroboros-v2.git
   cd Ouroboros-v2
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Run the guided setup (recommended for first-time users):**
   ```bash
   cd app/src/Ouroboros.CLI
   dotnet run -- setup --all
   ```

   The guided setup wizard will help you:
   - Install and configure Ollama for local LLM execution
   - Setup authentication for external providers (OpenAI, Ollama Cloud)
   - Install MeTTa symbolic reasoning engine (optional)
   - Configure local vector database (Qdrant)

   You can also run individual setup steps:
   ```bash
   dotnet run -- setup --ollama           # Install Ollama only
   dotnet run -- setup --auth             # Configure authentication
   dotnet run -- setup --metta            # Install MeTTa
   dotnet run -- setup --vector-store     # Setup vector database
   ```

5. **Try the examples:**
   ```bash
   cd app/src/Ouroboros.Examples
   dotnet run
   ```

### Quick Start

#### Command Line Interface

The CLI provides several commands for interacting with the pipeline system. All commands should be run from the `app/src/Ouroboros.CLI` directory:

```bash
# Navigate to CLI directory
cd app/src/Ouroboros.CLI

# Ask a question
dotnet run -- ask -q "What is functional programming?"

# Ask with RAG (retrieval augmented generation)
dotnet run -- ask -q "What does the code do?" --rag

# Run a pipeline with DSL
dotnet run -- pipeline -d "SetTopic('AI') | UseDraft | UseCritique | UseImprove"

# List available pipeline tokens
dotnet run -- list

# Explain a pipeline DSL
dotnet run -- explain -d "SetTopic('test') | UseDraft"

# Run tests
dotnet run -- test --all

# Run smart model orchestrator
dotnet run -- orchestrator --goal "Explain functional programming"

# Run orchestrator with specific models
dotnet run -- orchestrator \
  --goal "Write a Python function for sorting" \
  --coder-model "codellama" \
  --reason-model "llama3"

# Run MeTTa orchestrator with symbolic reasoning
dotnet run -- metta --goal "Analyze data patterns and find insights"

# Run MeTTa orchestrator in plan-only mode
dotnet run -- metta --goal "Create a research plan" --plan-only

# DAG Operations (Phase 0 — Evolution Foundations)
# Create snapshot of pipeline branches
dotnet run -- dag --command snapshot --branch main

# Show metrics and latest epoch
dotnet run -- dag --command show

# Replay snapshot from file
dotnet run -- dag --command replay --input snapshot.json

# Validate snapshot integrity
dotnet run -- dag --command validate

# Evaluate retention policy (dry run)
dotnet run -- dag --command retention --max-age-days 30 --dry-run
```

#### Orchestrating Complex Tasks with Small Models

Ouroboros supports **intelligent model orchestration** that allows you to efficiently handle complex tasks by combining multiple small, specialized models. This approach is more cost-effective and often faster than using a single massive model.

**Key Features:**
- **Automatic Model Selection**: The `--router auto` flag intelligently routes sub-tasks to specialized models
- **Multi-Model Composition**: Combine general, coding, reasoning, and summarization models
- **Performance Tracking**: Use `--show-metrics` to monitor model usage and optimize selection

**Example: Complex Code Review with Small Models**

```bash
# Use multiple small models for different aspects of code review
dotnet run -- pipeline \
  -d "SetTopic('Code Review Best Practices') | UseDraft | UseCritique | UseImprove" \
  --router auto \
  --general-model phi3:mini \          # Fast general responses (2.3GB)
  --coder-model deepseek-coder:1.3b \  # Specialized code analysis (800MB)
  --reason-model qwen2.5:3b \          # Deep reasoning (2GB)
  --trace                              # See which model handles each step
```

**Recommended Small Model Combinations:**

1. **Balanced Setup** (5GB total):
   - `phi3:mini` - General purpose (2.3GB)
   - `qwen2.5:3b` - Complex reasoning (2GB)
   - `deepseek-coder:1.3b` - Code tasks (800MB)

2. **Ultra-Light Setup** (2.5GB total):
   - `tinyllama` - Quick responses (637MB)
   - `phi3:mini` - General tasks (2.3GB)

3. **Specialized Setup** (8GB total):
   - `llama3:8b` - Advanced reasoning (4.7GB)
   - `deepseek-coder:6.7b` - Professional coding (3.8GB)

Install recommended models:
```bash
ollama pull phi3:mini
ollama pull qwen2.5:3b
ollama pull deepseek-coder:1.3b
```

#### Web API (Kubernetes-Friendly Remoting)

The Web API provides REST endpoints for the same pipeline functionality, ideal for containerized and cloud-native deployments:

```bash
# Navigate to Web API directory
cd app/src/Ouroboros.WebApi

# Run locally
dotnet run

# Or use Docker
docker-compose up -d monadic-pipeline-webapi
```

Access the API at `http://localhost:8080` with Swagger UI at the root `/`.

**API Examples:**

```bash
# Ask a question
curl -X POST http://localhost:8080/api/ask \
  -H "Content-Type: application/json" \
  -d '{
    "question": "What is functional programming?",
    "useRag": false,
    "model": "llama3"
  }'

# Execute a pipeline
curl -X POST http://localhost:8080/api/pipeline \
  -H "Content-Type: application/json" \
  -d '{
    "dsl": "SetTopic(\"AI\") | UseDraft | UseCritique",
    "model": "llama3"
  }'

# Health check (for Kubernetes)
curl http://localhost:8080/health
```

See [Web API Documentation](app/src/Ouroboros.WebApi/README.md) for more details.

#### Android App (Mobile CLI Interface)

Ouroboros is now available as an Android app with a terminal-style CLI interface and integrated Ollama support.

**Get the APK:**
- **Download:** APK is automatically built by CI/CD - download from [GitHub Actions artifacts](../../actions/workflows/android-build.yml)
- **Build locally:** Requires MAUI workload (see below)

```bash
# To build locally (requires: dotnet workload install maui-android)
cd app/src/Ouroboros.Android
dotnet build -c Release -f net10.0-android

# Install on connected device
dotnet build -c Release -f net10.0-android -t:Install
```

**Features:**
- **Terminal-Style UI**: Green-on-black terminal interface for mobile
- **Ollama Integration**: Connect to local or remote Ollama servers
- **Automatic Model Management**: Models auto-unload after 5 minutes of inactivity
- **Small Model Optimization**: Recommended models (tinyllama, phi, qwen, gemma)
- **Efficiency Hints**: Built-in guidance for battery, network, and memory usage
- **Standalone Operation**: Download models as needed from Ollama

**Quick Start on Android:**
1. Download and install the APK from GitHub Actions artifacts
2. Launch the app
3. Configure Ollama endpoint: `config http://YOUR_SERVER_IP:11434`
4. Pull a small model on your server: `ollama pull tinyllama`
5. Ask questions: `ask What is functional programming?`
6. See [Android App Documentation](app/src/Ouroboros.Android/README.md) for complete instructions.

#### Using Remote Endpoints (Ollama Cloud, OpenAI, Anthropic, GitHub Models, LiteLLM)

Configure remote AI endpoints via environment variables or CLI flags. All CLI commands (ask, pipeline, orchestrator, metta) support remote endpoints with authentication:

```bash
# Navigate to CLI directory
cd app/src/Ouroboros.CLI

# Set environment variables (recommended)
export CHAT_ENDPOINT="https://api.ollama.com"
export CHAT_API_KEY="your-api-key"
export CHAT_ENDPOINT_TYPE="ollama-cloud"  # or "auto", "openai", "anthropic", "github-models", "litellm"

# Anthropic Claude example
export ANTHROPIC_API_KEY="sk-ant-api03-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export CHAT_ENDPOINT="https://api.anthropic.com"
export CHAT_ENDPOINT_TYPE="anthropic"

# GitHub Models example
export GITHUB_TOKEN="ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export CHAT_ENDPOINT="https://models.inference.ai.azure.com"
export CHAT_ENDPOINT_TYPE="github-models"

# Now run any command
dotnet run -- ask -q "Hello"
dotnet run -- orchestrator --goal "Explain monads"
dotnet run -- metta --goal "Plan a research task"
```

**Provider Documentation:**

- **[Anthropic Claude Integration Guide](engine/docs/ANTHROPIC_INTEGRATION.md)** - Setting up Anthropic Claude API with secure key management
- **[Ollama Cloud Integration Guide](engine/docs/OLLAMA_CLOUD_INTEGRATION.md)** - Detailed guide for connecting to cloud-hosted Ollama instances
- **[GitHub Models Guide](engine/docs/GITHUB_MODELS_INTEGRATION.md)** - Using GitHub Models (GPT-4o, Llama 3) with Ouroboros

#### Cost Tracking

Ouroboros includes built-in LLM cost tracking across all providers. Enable cost visibility with CLI flags:

```bash
# Show cost after each response
dotnet run -- ask -q "What is a monad?" --show-costs

# Enable cost-aware prompts (model considers token efficiency)
dotnet run -- ask -q "Explain functional programming" --cost-aware

# Display session cost summary on exit
dotnet run -- ask -q "Deep analysis" --cost-summary

# Combine all cost options
dotnet run -- orchestrator --goal "Complex task" \
  --show-costs --cost-aware --cost-summary
```

**Supported Providers for Cost Tracking:**
- **Anthropic**: Claude 3/4/5 family (Opus, Sonnet, Haiku)
- **OpenAI**: GPT-4, GPT-4o, GPT-3.5 family
- **DeepSeek**: DeepSeek-Coder, DeepSeek-V2
- **Google**: Gemini Pro, Gemini Flash
- **Mistral**: Mistral Large, Medium, Small
- **Local**: Ollama/local models (tracked as free)

#### Programmatic Usage - Convenience Layer

The `Ouroboros` convenience class provides simplified access to the system's capabilities:

```csharp
// 1. Initialize with specific models
var ai = Ouroboros.Create(
    chatModelName: "llama3",
    embeddingModelName: "nomic-embed-text"
);

// 2. Or initialize with an existing specialized orchestrator
var ai = Ouroboros.Create(orchestrator);

// 3. Ask a simple question
var answer = await ai.AskAsync("What is a monad?");

// 4. Ask with RAG (automatically ingests/retrieves from vector store)
var ragAnswer = await ai.AskWithRagAsync("How does the current project handle errors?");

// 5. Execute a complex goal using the smart planner
var result = await ai.ExecuteGoalAsync("Analyze the project structure and suggest improvements");

// 6. Execute a specific pipeline DSL
var dslResult = await ai.ExecutePipelineAsync("SetTopic('FP') | UseDraft | UseCritique");
```

#### Programmatic Usage - Core Pipeline Composition

For advanced scenarios, you can compose pipelines directly:

```csharp
// Initialize core components
var llm = new ToolAwareChatModel(new OllamaChatConfig { ModelId = "llama3" });
var tools = new ToolRegistry();
var embed = new OllamaEmbeddingModel();

// Create reasoning arrows (Kleisli arrows)
var draft = ReasoningArrows.DraftArrow(llm, tools, embed, "Explain monads", "draft");
var critique = ReasoningArrows.CritiqueArrow(llm, tools, embed, "draft", "critique");
var improve = ReasoningArrows.ImproveArrow(llm, tools, embed, "critique", "final");

// Compose the pipeline
// Draft -> Critique -> Improve
var pipeline = draft.ComposeWith(critique).ComposeWith(improve);

// Execute
var result = await pipeline(ReasoningState.Initial);
```

## Core Concepts

### Monads

The project uses `Result<T>` and `Option<T>` to handle side effects and failures gracefully, ensuring that pipeline steps can be composed without exception handling boilerplate.

### Kleisli Arrows

Pipeline steps are modeled as Kleisli arrows `A -> M<B>`, where `M` is the `Result` monad. This allows for mathematical composition of steps: `(A -> M<B>) >=> (B -> M<C>)` yields `A -> M<C>`.

### Pipeline Composition

Pipelines are constructed by composing these arrows. If any step fails, the failure propagates through the monad, short-circuiting subsequent steps safely.

## Project Structure

| Layer | Projects | Location |
|-------|----------|----------|
| **Foundation** | Core, Domain, Tools, Genetic, Roslynator | `foundation/src/` |
| **Engine** | Agent, Pipeline, Providers, Network | `engine/src/` |
| **App** | Application, CLI, WebApi, Android, Easy, Examples | `app/src/` |
| **Build** | TestKit, CI templates, build config | `.build/` |

## Development

### Testing

```bash
# Run all tests from meta-repo
dotnet test

# Run specific layer tests
dotnet test foundation/tests/**/*.csproj
dotnet test engine/tests/**/*.csproj
dotnet test app/tests/**/*.csproj
```

### Mutation Testing

We use Stryker for mutation testing to ensure test quality:
```bash
dotnet tool install -g dotnet-stryker
```

### Working with Submodules

```bash
# Clone everything
git clone --recurse-submodules https://github.com/PMeeske/Ouroboros-v2.git

# Update all submodules
git submodule update --remote --merge

# Work on a specific layer
cd foundation && claude    # AI sees only foundation code
cd engine && claude        # AI sees engine code
```

## Documentation

### Essential Guides
- **[Quick Start Guide](app/docs/QUICKSTART.md)** - Get up and running in 5 minutes
- **[Deployment Guide](app/docs/DEPLOYMENT.md)** - Production deployment instructions
- **[Configuration & Security](app/docs/CONFIGURATION_AND_SECURITY.md)** - Configuration and security options
- **[Contributing Guide](.build/CONTRIBUTING.md)** - Guidelines for contributors

### Technical Documentation
- **[Architecture Overview](engine/docs/ARCHITECTURE.md)** - High-level system design
- **[Architectural Layers](engine/docs/ARCHITECTURAL_LAYERS.md)** - Detailed layer breakdown
- **[Recursive Chunking](foundation/docs/RECURSIVE_CHUNKING.md)** - Large context processing
- **[Self-Improving Agent](engine/docs/SELF_IMPROVING_AGENT.md)** - Agent capabilities
- **[Iterative Refinement](engine/docs/ITERATIVE_REFINEMENT_ARCHITECTURE.md)** - Reasoning loops

### Developer Resources
- **[Test Coverage Report](.build/TEST_COVERAGE_REPORT.md)** - Current test metrics
- **[Test Coverage Quick Reference](.build/TEST_COVERAGE_QUICKREF.md)** - Testing commands
- **[Infrastructure Dependencies](app/docs/INFRASTRUCTURE_DEPENDENCIES.md)** - System dependencies
- **[Troubleshooting](app/docs/TROUBLESHOOTING.md)** - Common issues and solutions

## Requirements

- .NET 10.0 SDK
- Ollama (for local LLM execution)
- Docker (optional, for containerized execution)

## Deployment

### Docker Deployment

```bash
docker build -t ouroboros -f app/deploy/Dockerfile.cli .
docker run -p 8080:8080 ouroboros
```

### Kubernetes Deployment

See [IONOS Deployment Guide](app/docs/IONOS_DEPLOYMENT_GUIDE.md) for Kubernetes deployment.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [LangChain](https://github.com/langchain-ai/langchain) for the inspiration
- [Ollama](https://ollama.ai/) for making local LLMs accessible
- [MeTTa](https://github.com/trueagi-io/hyperon-experimental) for symbolic reasoning capabilities
