# Contributing to Ouroboros-v2

For the full contributing guide, see [.build/docs/CONTRIBUTING.md](.build/docs/CONTRIBUTING.md).

## Quick Start

### Clone with Submodules

```bash
git clone --recurse-submodules https://github.com/your-org/Ouroboros-v2.git
cd Ouroboros-v2
```

If you already cloned without `--recurse-submodules`:

```bash
git submodule update --init --recursive
```

### Submodule Structure

| Submodule | Path | Purpose |
|-----------|------|---------|
| `.build` | `.build/` | Build system, CI/CD, shared props |
| `foundation` | `foundation/` | Core abstractions, monads, ethics |
| `engine` | `engine/` | Agent, MetaAI, NeuralSymbolic |
| `hypergrid` | `hypergrid/` | Hyperdimensional grid, thought streams, mesh |
| `iaret` | `iaret/` | Avatar identity, assets, holographic tools |
| `app` | `app/` | WebAPI, CLI, application layer |

### Common Submodule Operations

**Update all submodules to latest:**
```bash
git submodule update --remote --merge
```

**Check submodule status:**
```bash
git submodule status
```

### Troubleshooting

**Detached HEAD in submodule:**
```bash
cd <submodule-path>
git checkout main
git pull origin main
```

**Submodule out of sync:**
```bash
git submodule sync --recursive
git submodule update --init --recursive
```

**Complete submodule reset:**
```bash
git submodule deinit --all -f
git submodule update --init --recursive
```

### Build & Test

```bash
dotnet build Ouroboros.slnx
dotnet test foundation/tests/
dotnet test engine/tests/
dotnet test hypergrid/tests/
```
