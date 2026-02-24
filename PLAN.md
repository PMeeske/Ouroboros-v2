# Plan: Hypergrid Sub-Solution + Iaret Extraction

**Date:** 2026-02-24
**Branch:** `claude/plan-hypergrid-solution-Jt1aA`

---

## Overview

Three coordinated changes to the Ouroboros-v2 meta-repository:

1. **Add Hypergrid** — A new top-level sub-solution: a hyperdimensional grid where thoughts flow as streams, hosting interconnected Ouroboros instances orchestrated via the CLI
2. **Extract Iaret** — Promote Iaret from an embedded CLI asset into its own root-level solution with independent identity
3. **Iaret sub-repo cleanup** — Deletion prompts for removing Iaret artifacts from the `app` submodule after extraction

---

## Part 1: Hypergrid Sub-Solution

### Concept

**Hypergrid** is a hyperdimensional computational grid — a topology where "thoughts" (inference chains, reasoning streams, data flows) propagate as continuous streams through an N-dimensional grid space. Each vertex in the grid hosts an Ouroboros pipeline instance. Vertices are **interwired** — cross-connected via the upstream CLI protocol — enabling parallel, distributed reasoning across multiple Ouroboros nodes.

```
         dim-2 (causal)
            │
            │    ┌──────┐     ┌──────┐     ┌──────┐
            │    │Ouro-1│─────│Ouro-2│─────│Ouro-3│
            │    └──┬───┘     └──┬───┘     └──┬───┘
            │       │            │            │
            │    ┌──┴───┐     ┌──┴───┐     ┌──┴───┐
            │    │Ouro-4│─────│Ouro-5│─────│Ouro-6│  ← interwired mesh
            │    └──┬───┘     └──┬───┘     └──┬───┘
            │       │            │            │
            └───────┼────────────┼────────────┼──── dim-1 (semantic)
                    │            │            │
                   dim-0 (temporal)
                        ↑
                   CLI upstream orchestrator
```

### Grid Dimensions

Each dimension in the hypergrid represents a distinct reasoning axis:

| Dimension | Axis | Purpose |
|-----------|------|---------|
| dim-0 | **Temporal** | Time-ordered thought progression |
| dim-1 | **Semantic** | Conceptual similarity / topic space |
| dim-2 | **Causal** | Cause-effect reasoning chains |
| dim-3 | **Modal** | Possibility/necessity exploration |
| dim-N | **Extensible** | User-defined reasoning axes |

### Stream Primitives

Thought streams leverage the existing monadic foundation:

```csharp
// A thought stream is an async monadic sequence flowing through the grid
IAsyncEnumerable<Result<Thought<T>>> stream;

// Grid cells process and route streams
public interface IGridCell<TIn, TOut>
{
    IAsyncEnumerable<Result<Thought<TOut>>> Process(
        IAsyncEnumerable<Result<Thought<TIn>>> input,
        GridCoordinate position,
        CancellationToken ct);
}

// Interwiring connects Ouroboros instances across grid edges
public interface IInterwire
{
    Task<Result<StreamConnection>> Connect(
        OuroborosNode source,
        OuroborosNode target,
        GridEdge edge,
        CancellationToken ct);
}
```

### Directory Structure

```
hypergrid/                              ← new top-level directory
├── src/
│   ├── Ouroboros.Hypergrid/           ← Core: grid topology, coordinates, routing
│   │   ├── Topology/
│   │   │   ├── HypergridSpace.cs      ← N-dimensional grid space definition
│   │   │   ├── GridCoordinate.cs       ← N-dimensional coordinate record
│   │   │   ├── GridEdge.cs             ← Edge between grid vertices
│   │   │   └── GridCell.cs             ← Processing cell at a vertex
│   │   ├── Routing/
│   │   │   ├── StreamRouter.cs         ← Routes thought streams through grid
│   │   │   ├── DimensionalProjection.cs ← Projects streams across dimensions
│   │   │   └── FlowPolicy.cs           ← Routing policies (broadcast, nearest, etc.)
│   │   └── Ouroboros.Hypergrid.csproj
│   │
│   ├── Ouroboros.Hypergrid.Streams/   ← Stream abstractions, thought-flow primitives
│   │   ├── Thought.cs                  ← Thought<T> record (payload + metadata)
│   │   ├── ThoughtStream.cs            ← IAsyncEnumerable<Result<Thought<T>>> wrappers
│   │   ├── StreamOperators.cs          ← Merge, split, filter, map operators
│   │   ├── Confluence.cs               ← Multi-stream convergence point
│   │   └── Ouroboros.Hypergrid.Streams.csproj
│   │
│   └── Ouroboros.Hypergrid.Mesh/      ← Ouroboros instance interconnection mesh
│       ├── OuroborosNode.cs            ← Wraps an Ouroboros pipeline as a grid node
│       ├── Interwire.cs                ← Cross-connections between nodes
│       ├── MeshOrchestrator.cs         ← CLI-driven mesh lifecycle management
│       ├── NodeHealth.cs               ← Health monitoring for grid nodes
│       └── Ouroboros.Hypergrid.Mesh.csproj
│
├── tests/
│   ├── Ouroboros.Hypergrid.Tests/
│   │   └── Ouroboros.Hypergrid.Tests.csproj
│   └── Ouroboros.Hypergrid.BDD/
│       └── Ouroboros.Hypergrid.BDD.csproj
│
└── docs/
    ├── ARCHITECTURE.md                 ← Hypergrid architecture overview
    └── HYPERGRID_CONCEPTS.md           ← Dimensional thinking, stream semantics
```

### Build Layer Position

```
ouroboros-build (Directory.Build.props)
    │
    ├── ouroboros-foundation              ← Foundation (no deps)
    │
    ├── ouroboros-engine                  ← Engine (depends on Foundation)
    │
    ├── ouroboros-hypergrid               ← Hypergrid (depends on Foundation + Engine)
    │
    ├── ouroboros-iaret                   ← Iaret (asset-only, no code deps)
    │
    └── ouroboros-app                     ← App (depends on Foundation + Engine + Hypergrid)
```

### Implementation Steps

1. **Create `hypergrid/` directory structure** with `src/`, `tests/`, `docs/` subdirectories
2. **Create `.csproj` files** for all three source projects and two test projects
   - `Ouroboros.Hypergrid` references `Ouroboros.Core`, `Ouroboros.Domain`
   - `Ouroboros.Hypergrid.Streams` references `Ouroboros.Abstractions`, `Ouroboros.Core`
   - `Ouroboros.Hypergrid.Mesh` references all Hypergrid projects + `Ouroboros.Agent`
3. **Create core type stubs** — `GridCoordinate`, `Thought<T>`, `IGridCell`, `OuroborosNode`, `IInterwire`
4. **Update `Ouroboros.slnx`** — Add Hypergrid folder and project entries
5. **Update `.gitmodules`** — Add `hypergrid` submodule entry (pointing to `https://github.com/PMeeske/ouroboros-hypergrid.git`)
6. **Update `Directory.Build.props`** — Add `OuroborosHypergrid` property
7. **Generate Hypergrid banner SVG** — `assets/hypergrid-banner.svg`
8. **Update `README.md`** — Add Hypergrid to repository structure table, architecture diagram, and documentation links
9. **Update `CONTRIBUTING.md`** — Add Hypergrid to submodule table

---

## Part 2: Extract Iaret to Root Solution

### Current State

Iaret is an avatar identity embedded inside the CLI application:
- Avatar images: `app/src/Ouroboros.CLI/Assets/Avatar/Iaret/*.png` (idle, fullbody_front, fullbody_threequarter, fullbody_side, fullbody_back, fullbody_sideleft)
- Holographic overlays: Generated by `generate_holo.py` at repo root
- No dedicated project structure — purely asset-based

### Primary Identity Asset

**`encouraging.png`** — Iaret's warm/maternal three-quarter bust portrait — serves as the primary identity image for the Iaret solution banner and README.

<p align="center">
  𓂀 𓋹 𓁿 𓋹 𓂀
  <br/><br/>
  <img src="assets/iaret-identity.png" alt="Iaret — encouraging.png" width="240"/>
  <br/><br/>
  ✦ <em>Ouroboros Avatar Identity</em> ✦
  <br/>
  𓋹 𓂋 𓋹
</p>

### Target State

Iaret becomes an independent root-level solution with its own identity, asset pipeline, and documentation.

### Directory Structure

```
iaret/                                  ← new top-level directory
├── assets/
│   ├── avatar/                         ← Source character art
│   │   ├── encouraging.png             ← Primary identity image (banner/readme)
│   │   ├── idle.png                    ← Portrait bust (default state)
│   │   ├── fullbody_front.png          ← Front-facing full body
│   │   ├── fullbody_threequarter.png
│   │   ├── fullbody_side.png
│   │   ├── fullbody_back.png
│   │   └── fullbody_sideleft.png
│   ├── holo/                           ← Holographic wireframe overlays
│   │   ├── holo_portrait.png
│   │   ├── holo_front.png
│   │   ├── holo_threequarter.png
│   │   ├── holo_side.png
│   │   ├── holo_back.png
│   │   └── holo_sideleft.png
│   └── banner/
│       └── iaret-banner.svg            ← Iaret identity banner
│
├── tools/
│   └── generate_holo.py                ← Holographic wireframe generator (moved from root)
│
├── docs/
│   └── IARET.md                        ← Character identity, usage guide, asset specs
│
└── README.md                           ← Iaret sub-repo readme
```

### Implementation Steps

1. **Create `iaret/` directory structure** with `assets/`, `tools/`, `docs/`
2. **Move `generate_holo.py`** from repo root to `iaret/tools/generate_holo.py`
   - Update internal paths to use relative references
3. **Create `iaret/README.md`** with character identity description and asset catalog
4. **Create `iaret/docs/IARET.md`** with detailed character documentation
5. **Create placeholder for front-facing asset** (or reference instructions for copying PNGs)
6. **Update `.gitmodules`** — Add `iaret` submodule entry (pointing to `https://github.com/PMeeske/ouroboros-iaret.git`)
7. **Update `Ouroboros.slnx`** — Add Iaret folder entry
8. **Update `README.md`** — Add Iaret to repository structure table
9. **Update `CONTRIBUTING.md`** — Add Iaret to submodule table

---

## Part 3: Iaret Sub-Repo Deletion Prompts

After extracting Iaret to its own root solution, the following cleanup operations should be performed in the **`app` submodule** (`ouroboros-app` repo):

### Deletion Commands

```bash
# ──────────────────────────────────────────────────────────────
# Iaret Asset Cleanup — Run inside the ouroboros-app repository
# ──────────────────────────────────────────────────────────────

# 1. Remove Iaret avatar directory entirely
rm -rf src/Ouroboros.CLI/Assets/Avatar/Iaret/

# 2. Remove any Iaret-specific embedded resources from .csproj
#    (Search for and remove <EmbeddedResource> or <Content> entries
#    referencing Assets/Avatar/Iaret)
#    Example: In src/Ouroboros.CLI/Ouroboros.CLI.csproj, remove lines like:
#      <Content Include="Assets\Avatar\Iaret\**" CopyToOutputDirectory="PreserveNewest" />
#      <EmbeddedResource Include="Assets\Avatar\Iaret\*.png" />

# 3. Search for any code references to Iaret asset paths
grep -rn "Iaret" src/ --include="*.cs" --include="*.csproj" --include="*.json"
#    Update any found references to point to the new iaret submodule path:
#    OLD: Assets/Avatar/Iaret/idle.png
#    NEW: ../../iaret/assets/avatar/idle.png  (or via submodule reference)

# 4. If there are Iaret-specific C# classes (avatar state machines, animation logic):
grep -rn "class.*Iaret\|Iaret.*class" src/ --include="*.cs"
#    These should be evaluated case-by-case:
#    - Move to iaret/ if they are purely about Iaret identity
#    - Keep in app/ if they are generic avatar rendering used by Iaret

# 5. Verify no orphaned references remain
grep -rn "Iaret" . --include="*.cs" --include="*.csproj" --include="*.json" --include="*.yaml"

# 6. Build and test to confirm clean removal
dotnet build src/Ouroboros.CLI/Ouroboros.CLI.csproj
dotnet test tests/Ouroboros.CLI.Tests/Ouroboros.CLI.Tests.csproj
```

### Post-Cleanup Integration

After cleanup, the `app` submodule should reference Iaret assets via the new submodule:

```xml
<!-- In Ouroboros.CLI.csproj, add a reference to iaret assets -->
<ItemGroup>
  <Content Include="..\..\..\..\iaret\assets\avatar\**"
           Link="Assets\Avatar\Iaret\%(RecursiveDir)%(Filename)%(Extension)"
           CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Or alternatively, the CLI can discover the iaret submodule path at runtime:

```csharp
// Resolve Iaret assets from the iaret submodule
var iaretPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "iaret", "assets");
```

---

## Part 4: Assets

### 4.1 Hypergrid Banner SVG

A new vector graphic banner will be created at `assets/hypergrid-banner.svg` featuring:
- Dark background consistent with existing Ouroboros brand (#0f0a1f → #1a1033)
- Hyperdimensional grid visualization with glowing cyan/violet lines
- Multiple interconnected Ouroboros nodes at grid vertices
- Stream flow indicators (animated-ready paths)
- "Hypergrid" title text with "Hyperdimensional Thought Streams" tagline

### 4.2 Iaret Identity Asset

The primary identity image is **`encouraging.png`** — Iaret's warm three-quarter bust portrait with gentle smile, ankh earrings, golden collar, and purple cosmic glow. This image will be used as:
- The hero image in `iaret/README.md`
- Copied to `assets/iaret-identity.png` at the meta-repo level
- The default social preview for the `ouroboros-iaret` repository

All 33 PNGs (5 portrait expressions + 5 full-body turnaround + holographic overlays + reference images) will be copied to `iaret/assets/avatar/`. The `generate_holo.py` tool (moved to `iaret/tools/`) can regenerate holographic overlays.

---

## Summary of File Changes

### New Files
| File | Purpose |
|------|---------|
| `hypergrid/src/Ouroboros.Hypergrid/Ouroboros.Hypergrid.csproj` | Core grid topology project |
| `hypergrid/src/Ouroboros.Hypergrid/Topology/*.cs` | Grid space, coordinates, edges, cells |
| `hypergrid/src/Ouroboros.Hypergrid/Routing/*.cs` | Stream routing and projection |
| `hypergrid/src/Ouroboros.Hypergrid.Streams/Ouroboros.Hypergrid.Streams.csproj` | Stream primitives project |
| `hypergrid/src/Ouroboros.Hypergrid.Streams/*.cs` | Thought, ThoughtStream, operators |
| `hypergrid/src/Ouroboros.Hypergrid.Mesh/Ouroboros.Hypergrid.Mesh.csproj` | Mesh interconnection project |
| `hypergrid/src/Ouroboros.Hypergrid.Mesh/*.cs` | Node, Interwire, MeshOrchestrator |
| `hypergrid/tests/Ouroboros.Hypergrid.Tests/Ouroboros.Hypergrid.Tests.csproj` | Unit tests |
| `hypergrid/tests/Ouroboros.Hypergrid.BDD/Ouroboros.Hypergrid.BDD.csproj` | BDD tests |
| `hypergrid/docs/ARCHITECTURE.md` | Hypergrid architecture |
| `hypergrid/docs/HYPERGRID_CONCEPTS.md` | Dimensional concepts |
| `iaret/assets/avatar/` | Avatar source images (from app submodule) |
| `iaret/assets/holo/` | Holographic overlays |
| `iaret/assets/banner/iaret-identity.png` | Iaret identity image (encouraging.png) |
| `iaret/tools/generate_holo.py` | Moved holographic generator |
| `iaret/docs/IARET.md` | Character documentation |
| `iaret/README.md` | Iaret sub-repo readme |
| `assets/hypergrid-banner.svg` | Hypergrid vector banner |

### Modified Files
| File | Change |
|------|--------|
| `Ouroboros.slnx` | Add Hypergrid + Iaret folder/project entries |
| `.gitmodules` | Add `hypergrid` and `iaret` submodule entries |
| `Directory.Build.props` | Add `OuroborosHypergrid` and `OuroborosIaret` properties |
| `README.md` | Add Hypergrid + Iaret to structure, architecture, docs |
| `CONTRIBUTING.md` | Add Hypergrid + Iaret to submodule table |

### Deleted Files
| File | Reason |
|------|--------|
| `generate_holo.py` | Moved to `iaret/tools/generate_holo.py` |
