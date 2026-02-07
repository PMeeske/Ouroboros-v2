# Ouroboros

The Ouroboros meta-repo — a functional programming-based AI pipeline system built on LangChain.

## Structure

This repository uses **git submodules** to compose the full system from independent sub-repos:

| Submodule | Repository | Description |
|-----------|-----------|-------------|
| `.build/` | [ouroboros-build](https://github.com/PMeeske/ouroboros-build) | Shared build config, CI templates, TestKit |
| `foundation/` | [ouroboros-foundation](https://github.com/PMeeske/ouroboros-foundation) | Core, Domain, Tools, Genetic, Roslynator |
| `engine/` | [ouroboros-engine](https://github.com/PMeeske/ouroboros-engine) | Agent, Pipeline, Providers, Network |
| `app/` | [ouroboros-app](https://github.com/PMeeske/ouroboros-app) | Application, CLI, WebApi, Android, Easy |

## Quick Start

```bash
# Clone with all submodules
git clone --recurse-submodules https://github.com/PMeeske/Ouroboros-v2.git
cd Ouroboros-v2

# Build everything
dotnet build Ouroboros.sln

# Run all tests
dotnet test Ouroboros.sln

# Run specific layer tests
dotnet test foundation/tests/**/*.csproj
dotnet test engine/tests/**/*.csproj
dotnet test app/tests/**/*.csproj
```

## Build Inheritance

```
ouroboros-build (Directory.Build.props)         ← Base: analyzers, lang version, warnings
    │
    ├── ouroboros-foundation                    ← Layer: Foundation (no upstream deps)
    │
    ├── ouroboros-engine                        ← Layer: Engine (depends on Foundation)
    │
    └── ouroboros-app                           ← Layer: App (depends on Foundation + Engine)
```

All sub-repos inherit shared build config from `ouroboros-build` via the root `Directory.Build.props`.

## Development Workflow

Each sub-repo can be developed independently:
- **Foundation**: Pure functional primitives — no external dependencies
- **Engine**: AI runtime — depends on Foundation via `$(OuroborosFoundation)` MSBuild variable
- **App**: Hosts + integration — depends on Foundation + Engine

For full-system work, use this meta-repo which wires everything together via submodules.

## Technologies

- .NET 10.0, C# 14
- Category Theory / Monadic Composition
- LangChain, Vector Databases
- xUnit, FluentAssertions, Stryker.NET
- GitHub Actions (reusable workflows)
