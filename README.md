<p align="center">
  <img src="assets/user-banner.svg" alt="Ouroboros — Functional AI Pipeline System" width="100%">
</p>

<p align="center">
  <a href="https://github.com/PMeeske/ouroboros-foundation/actions/workflows/ci.yml">Foundation:<img src="https://github.com/PMeeske/ouroboros-foundation/actions/workflows/ci.yml/badge.svg" alt="Foundation CI"></a></br>
  <a href="https://github.com/PMeeske/ouroboros-foundation/actions/workflows/mutation.yml">Foundation:<img src="https://github.com/PMeeske/ouroboros-foundation/actions/workflows/mutation.yml/badge.svg" alt="Foundation Mutation"></a></br>
  <a href="https://github.com/PMeeske/ouroboros-engine/actions/workflows/ci.yml">Engine:<img src="https://github.com/PMeeske/ouroboros-engine/actions/workflows/ci.yml/badge.svg" alt="Engine CI"></a></br>
  <a href="https://github.com/PMeeske/ouroboros-engine/actions/workflows/mutation.yml">Engine:<img src="https://github.com/PMeeske/ouroboros-engine/actions/workflows/mutation.yml/badge.svg" alt="Engine Mutation"></a></br>
  <a href="https://github.com/PMeeske/ouroboros-app/actions/workflows/ci.yml">App:<img src="https://github.com/PMeeske/ouroboros-app/actions/workflows/ci.yml/badge.svg" alt="App CI"></a></br>
  <a href="https://github.com/PMeeske/ouroboros-app/actions/workflows/mutation.yml">App:<img src="https://github.com/PMeeske/ouroboros-app/actions/workflows/mutation.yml/badge.svg" alt="App Mutation"></a></br>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-blue" alt=".NET Version"></a></br>
  <a href="https://www.nuget.org/packages/LangChain/"><img src="https://img.shields.io/badge/LangChain-0.17.0-purple" alt="LangChain"></a></br>
</p>

A **sophisticated functional programming-based AI pipeline system** (YET EXPERIMENTAL) built on LangChain, implementing category theory principles, monadic composition, and functional programming patterns to create robust, self-improving AI agents.

##### The inner value compass of this repo: 
  ###### -> https://www.gnu.org/music/free-software-song.html (sorry for the minor inconsistency) 😂
  ###### -> Liberty 
  ###### -> Compassion for humanity and the living
  ###### -> Real understanding/leaning beyond politics
  ###### -> Trying to solve the *real* (also my own) problems our time with my limited capabilities/ressources
  ###### -> Sorry for some broken links. Will repair it asap. 

## Repository Structure

This repository uses **git submodules** to compose the full system from independent sub-repos:

| Submodule | Repository | Description |
|-----------|-----------|-------------|
| `.build/` | [ouroboros-build](https://github.com/PMeeske/ouroboros-build) | Shared build config, CI templates, TestKit |
| `foundation/` | [ouroboros-foundation](https://github.com/PMeeske/ouroboros-foundation) | Core, Domain, Tools, Genetic, Roslynator |
| `engine/` | [ouroboros-engine](https://github.com/PMeeske/ouroboros-engine) | Agent, Pipeline, Providers, Network |
| `hypergrid/` | [ouroboros-hypergrid](https://github.com/PMeeske/ouroboros-hypergrid) | Hyperdimensional grid topology, thought streams, mesh |
| `iaret/` | [ouroboros-iaret](https://github.com/PMeeske/ouroboros-iaret) | Iaret avatar identity, assets, holographic tools |
| `app/` | [ouroboros-app](https://github.com/PMeeske/ouroboros-app) | Application, CLI, WebApi, Android, Easy |

### Build Inheritance

```
ouroboros-build (Directory.Build.props)         ← Base: analyzers, lang version, warnings
    │
    ├── ouroboros-foundation                    ← Layer: Foundation (no upstream deps)
    │
    ├── ouroboros-engine                        ← Layer: Engine (depends on Foundation)
    │
    ├── ouroboros-iaret                         ← Layer: Iaret identity (asset-only)
    │
    ├── ouroboros-hypergrid                     ← Layer: Hypergrid (depends on Foundation + Engine)
    │
    └── ouroboros-app                           ← Layer: App (depends on Foundation + Engine)
```

## Key Features

- **Monadic Composition** — Type-safe pipeline operations using `Result<T>` and `Option<T>` monads
- **Kleisli Arrows** — Mathematical composition of computations in monadic contexts
- **LangChain Integration** — Native provider/tool integration with pipe operators (`Set | Retrieve | Template | LLM`)
- **Meta-AI Layer v2** — Planner/Executor/Verifier orchestrator with continual learning
- **Self-Improving Agents** — Automatic skill extraction and learning from successful executions
- **Phase 2 Metacognition** — Agent self-model, goal hierarchy, capability registry, self-evaluation
- **Integrated Self-Model** — Identity graph, global workspace, predictive monitoring, self-explanation
- **Epic Branch Orchestration** — Auto agent assignment, dedicated branches, parallel sub-issue processing
- **Phase 0 Evolution** — Feature flags, DAG maintenance, global projection service
- **MeTTa Symbolic Reasoning** — Hybrid neural-symbolic AI integration
- **Event Sourcing** — Complete immutable audit trail with replay
- **RecursiveChunkProcessor** — Adaptive chunking and map-reduce for large contexts (100+ pages)
- **Multi-Provider Support** — Anthropic, OpenAI, Ollama, GitHub Models, LiteLLM, and more
- **Cost Tracking** — Built-in LLM usage cost tracking with session summaries
- **IONOS Cloud Ready** — Optimized for Kubernetes deployment

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Core Layer    │    │  Domain Layer   │    │ Pipeline Layer  │
│ • Monads        │───▶│ • Events        │───▶│ • Branches      │
│ • Kleisli       │    │ • States        │    │ • Vectors       │
│ • Steps         │    │ • Vectors       │    │ • Ingestion     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 ▼
                    ┌─────────────────┐
                    │Integration Layer│
                    │ • Tools         │
                    │ • Providers     │
                    │ • Memory        │
                    └─────────────────┘
```

Pipeline steps are modeled as **Kleisli arrows** `A → M<B>` where `M` is the `Result` monad. Composition `(A → M<B>) >=> (B → M<C>)` yields `A → M<C>` — if any step fails, the failure propagates automatically.

See **[Architecture Overview](engine/docs/ARCHITECTURE.md)** and **[Architectural Layers](engine/docs/ARCHITECTURAL_LAYERS.md)** for full details.

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Ollama](https://ollama.ai/) (for local LLMs) or remote API access (optional)

### Clone & Build

```bash
git clone --recurse-submodules https://github.com/PMeeske/Ouroboros-v2.git
cd Ouroboros-v2
dotnet restore && dotnet build
```

### First-Time Setup

```bash
cd app/src/Ouroboros.CLI
dotnet run -- setup --all    # Guided wizard: Ollama, auth, MeTTa, vector DB
```

### Quick Usage

```bash
cd app/src/Ouroboros.CLI
dotnet run -- ask -q "What is functional programming?"
dotnet run -- pipeline -d "SetTopic('AI') | UseDraft | UseCritique | UseImprove"
dotnet run -- orchestrator --goal "Explain monads"
```

See **[Quick Start Guide](app/docs/QUICKSTART.md)** | **[CLI Examples](app/docs/OVERPOWERED_CLI_EXAMPLES.md)** | **[CLI Quick Reference](app/docs/CLI_METTA_QUICKREF.md)** for comprehensive CLI documentation.

### Programmatic Usage

```csharp
// Convenience layer
var ai = Ouroboros.Create(chatModelName: "llama3", embeddingModelName: "nomic-embed-text");
var answer = await ai.AskAsync("What is a monad?");
var ragAnswer = await ai.AskWithRagAsync("How does the project handle errors?");

// Core pipeline composition with Kleisli arrows
var draft = ReasoningArrows.DraftArrow(llm, tools, embed, "Explain monads", "draft");
var critique = ReasoningArrows.CritiqueArrow(llm, tools, embed, "draft", "critique");
var improve = ReasoningArrows.ImproveArrow(llm, tools, embed, "critique", "final");
var pipeline = draft.ComposeWith(critique).ComposeWith(improve);
var result = await pipeline(ReasoningState.Initial);
```

See **[Easy API Quick Start](app/docs/EASY_API_QUICKSTART.md)** for more programmatic examples.

## Configuration

⚠️ **Security Warning**: Never commit real credentials to the repository. Use secure configuration methods as described below.

Ouroboros supports multiple methods for configuring credentials and sensitive settings:

### Option 1: Development Configuration File (Recommended for Local Development)

1. Copy the example configuration:
   ```bash
   cp appsettings.Development.json.example appsettings.Development.json
   ```

2. Edit `appsettings.Development.json` with your actual credentials:
   ```json
   {
     "ApiKeys": {
       "OpenAI": "sk-...",
       "Anthropic": "sk-ant-...",
       "Firecrawl": "fc-...",
       "SerpApi": "..."
     },
     "Tapo": {
       "Username": "your-email@example.com",
       "Password": "your-password",
       "Devices": [
         {
           "name": "Camera1",
           "device_type": "C200",
           "ip_addr": "192.168.1.100"
         }
       ]
     }
   }
   ```

3. This file is automatically ignored by Git and will not be committed.

### Option 2: .NET User Secrets (Recommended for Development)

Use .NET's built-in secrets manager for development:

```bash
cd app/src/Ouroboros.CLI  # or any other project directory
dotnet user-secrets init
dotnet user-secrets set "ApiKeys:OpenAI" "sk-..."
dotnet user-secrets set "ApiKeys:Anthropic" "sk-ant-..."
dotnet user-secrets set "Tapo:Username" "your-email@example.com"
dotnet user-secrets set "Tapo:Password" "your-password"
```

User secrets are stored outside the repository in your user profile directory.

### Option 3: Environment Variables (Recommended for Production)

Set environment variables using your operating system or deployment platform:

```bash
export ApiKeys__OpenAI="sk-..."
export ApiKeys__Anthropic="sk-ant-..."
export Tapo__Username="your-email@example.com"
export Tapo__Password="your-password"
```

For Docker deployments, use the `docker-compose.yml` or Kubernetes secrets.

### Configuration Priority

.NET configuration sources are loaded in this order (later sources override earlier ones):

1. `appsettings.json` (base configuration with no credentials)
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. User Secrets (development only)
4. Environment Variables
5. Command-line arguments

## Project Structure

| Layer | Projects | Location |
|-------|----------|----------|
| **Foundation** | Core, Domain, Tools, Genetic, Roslynator | `foundation/src/` |
| **Engine** | Agent, Pipeline, Providers, Network | `engine/src/` |
| **Hypergrid** | Hypergrid, Hypergrid.Streams, Hypergrid.Mesh | `hypergrid/src/` |
| **Iaret** | Avatar identity, holographic tools | `iaret/` |
| **App** | Application, CLI, WebApi, Android, Easy, Examples | `app/src/` |
| **Build** | TestKit, CI templates, build config | `.build/` |

## Development

```bash
# Run all tests
dotnet test

# Run layer-specific tests
dotnet test foundation/tests/**/*.csproj
dotnet test engine/tests/**/*.csproj
dotnet test app/tests/**/*.csproj
dotnet test hypergrid/tests/**/*.csproj

# Work on a specific layer (isolated AI context)
cd foundation && claude
cd engine && claude
cd hypergrid && claude
```

### Working with Submodules

```bash
git submodule update --remote --merge   # Pull latest from all sub-repos
```

## Documentation

Each sub-repo contains its own `docs/` directory. Key guides:

### Getting Started
| Guide | Location |
|-------|----------|
| Quick Start | [app/docs/QUICKSTART.md](app/docs/QUICKSTART.md) |
| Configuration & Security | [app/docs/CONFIGURATION_AND_SECURITY.md](app/docs/CONFIGURATION_AND_SECURITY.md) |
| Troubleshooting | [app/docs/TROUBLESHOOTING.md](app/docs/TROUBLESHOOTING.md) |
| Contributing | [.build/docs/CONTRIBUTING.md](.build/docs/CONTRIBUTING.md) |

### Hypergrid
| Guide | Location |
|-------|----------|
| Hypergrid Architecture | [hypergrid/docs/ARCHITECTURE.md](hypergrid/docs/ARCHITECTURE.md) |
| Hypergrid Concepts | [hypergrid/docs/HYPERGRID_CONCEPTS.md](hypergrid/docs/HYPERGRID_CONCEPTS.md) |

### Iaret
| Guide | Location |
|-------|----------|
| Iaret Identity | [iaret/README.md](iaret/README.md) |
| Character Docs | [iaret/docs/IARET.md](iaret/docs/IARET.md) |
| Goal Project | [iaret/docs/GOAL_PROJECT.md](iaret/docs/GOAL_PROJECT.md) |

### Architecture & Design
| Guide | Location |
|-------|----------|
| Architecture Overview | [engine/docs/ARCHITECTURE.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/ARCHITECTURE.md) |
| Architectural Layers | [engine/docs/ARCHITECTURAL_LAYERS.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/ARCHITECTURAL_LAYERS.md) |
| Cognitive Architecture | [engine/docs/COGNITIVE_ARCHITECTURE.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/COGNITIVE_ARCHITECTURE.md) |
| Iterative Refinement | [engine/docs/ITERATIVE_REFINEMENT_ARCHITECTURE.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/ITERATIVE_REFINEMENT_ARCHITECTURE.md) |
| Self-Improving Agent | [engine/docs/SELF_IMPROVING_AGENT.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/SELF_IMPROVING_AGENT.md) |
| Arrow Composition | [foundation/docs/ARROW_COMPOSITION_EXAMPLES.md](https://github.com/PMeeske/ouroboros-foundation/tree/main/docs/ARROW_COMPOSITION_EXAMPLES.md) |
| Laws of Form | [foundation/docs/LAWS_OF_FORM.md](https://github.com/PMeeske/ouroboros-foundation/tree/main/docs/LAWS_OF_FORM.md) |

### Providers & Integration
| Guide | Location |
|-------|----------|
| Anthropic Claude | [engine/docs/ANTHROPIC_INTEGRATION.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/ANTHROPIC_INTEGRATION.md) |
| Ollama Cloud | [engine/docs/OLLAMA_CLOUD_INTEGRATION.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/OLLAMA_CLOUD_INTEGRATION.md) |
| GitHub Models | [engine/docs/GITHUB_MODELS_INTEGRATION.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/GITHUB_MODELS_INTEGRATION.md) |
| Vector Stores | [engine/docs/VECTOR_STORES.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/VECTOR_STORES.md) |
| MeTTa Neuro-Symbolic | [engine/docs/METTA_NEURO_SYMBOLIC_ARCHITECTURE.md](https://github.com/PMeeske/ouroboros-engine/tree/main/docs/METTA_NEURO_SYMBOLIC_ARCHITECTURE.md) |

### Deployment & Infrastructure
| Guide | Location |
|-------|----------|
| Deployment Guide | [app/docs/DEPLOYMENT.md](https://github.com/PMeeske/ouroboros-app/tree/main/docs/DEPLOYMENT.md) |
| Deployment Quick Reference | [app/docs/DEPLOYMENT-QUICK-REFERENCE.md](https://github.com/PMeeske/ouroboros-app/tree/main/docs/DEPLOYMENT-QUICK-REFERENCE.md) |
| IONOS Deployment | [app/docs/IONOS_DEPLOYMENT_GUIDE.md](https://github.com/PMeeske/ouroboros-app/tree/main/docs/IONOS_DEPLOYMENT_GUIDE.md) |
| Infrastructure Dependencies | [app/docs/INFRASTRUCTURE_DEPENDENCIES.md](https://github.com/PMeeske/ouroboros-app/tree/main/docs/INFRASTRUCTURE_DEPENDENCIES.md) |
| Terraform + K8s | [app/docs/TERRAFORM_K8S_INTEGRATION.md](https://github.com/PMeeske/ouroboros-app/tree/main/docs/TERRAFORM_K8S_INTEGRATION.md) |
| Android App | [app/src/Ouroboros.Android/README.md](https://github.com/PMeeske/ouroboros-app/tree/main/src/Ouroboros.Android/README.md) |
| Web API | [app/src/Ouroboros.WebApi/README.md](https://github.com/PMeeske/ouroboros-app/tree/main/src/Ouroboros.WebApi/README.md) |

### Testing & Quality
| Guide | Location |
|-------|----------|
| Test Coverage Report | [.build/docs/TEST_COVERAGE_REPORT.md](.build/docs/TEST_COVERAGE_REPORT.md) |
| Test Coverage Quick Ref | [.build/docs/TEST_COVERAGE_QUICKREF.md](.build/docs/TEST_COVERAGE_QUICKREF.md) |
| Mutation Testing Guide | [.build/docs/TEST_MUTATION_GUIDE.md](.build/docs/TEST_MUTATION_GUIDE.md) |
| Benchmark Suite | [foundation/docs/BENCHMARK_SUITE.md](foundation/docs/BENCHMARK_SUITE.md) |

## Requirements

- .NET 10.0 SDK
- Ollama (for local LLM execution)
- Docker (optional, for containerized execution)

## License

MIT — see [LICENSE](LICENSE).

## Acknowledgments

- [LangChain](https://github.com/langchain-ai/langchain) for the inspiration
- [Ollama](https://ollama.ai/) for making local LLMs accessible
- [MeTTa](https://github.com/trueagi-io/hyperon-experimental) for symbolic reasoning capabilities
