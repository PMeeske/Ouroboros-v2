# Hypergrid Architecture

## Overview

**Hypergrid** is a hyperdimensional computational grid — a topology where *thoughts* (inference chains, reasoning streams, data flows) propagate as continuous streams through an N-dimensional grid space. Each vertex in the grid hosts an Ouroboros pipeline instance. Vertices are **interwired** — cross-connected via the upstream CLI protocol — enabling parallel, distributed reasoning across multiple Ouroboros nodes.

## Architectural Layers

```
┌────────────────────────────────────────────────────────┐
│                  Ouroboros.Hypergrid.Mesh               │
│  MeshOrchestrator · OuroborosNode · Interwire          │
│  CLI-driven lifecycle, health monitoring               │
├────────────────────────────────────────────────────────┤
│                Ouroboros.Hypergrid.Streams              │
│  Thought<T> · ThoughtStream · StreamOperators          │
│  Confluence · IGridCell<TIn, TOut>                     │
├────────────────────────────────────────────────────────┤
│                  Ouroboros.Hypergrid                    │
│  HypergridSpace · GridCoordinate · GridEdge · GridCell │
│  StreamRouter · DimensionalProjection · FlowPolicy     │
├────────────────────────────────────────────────────────┤
│            Ouroboros Foundation + Engine                │
│  Core · Domain · Abstractions · Agent · Pipeline       │
└────────────────────────────────────────────────────────┘
```

### Ouroboros.Hypergrid (Core)

The foundational topology layer defines the N-dimensional grid space:

- **HypergridSpace** — Root object holding dimensions, cells, and edges
- **GridCoordinate** — Immutable N-dimensional position record
- **GridEdge** — Directed, weighted edge between vertices with dimension metadata
- **GridCell** — Processing cell at a vertex, wrapping an Ouroboros pipeline
- **StreamRouter** — Routes thought streams based on a FlowPolicy
- **DimensionalProjection** — Projects coordinates across dimensions
- **FlowPolicy** — Routing strategy (broadcast, nearest, dimensional)

### Ouroboros.Hypergrid.Streams

Stream abstractions for thought-flow primitives:

- **Thought\<T\>** — A discrete reasoning unit with payload, origin coordinate, timestamp, and trace metadata
- **ThoughtStream** — Factory/extensions for `IAsyncEnumerable<Thought<T>>` creation and composition
- **StreamOperators** — Merge, split, filter, and map operators for thought streams
- **Confluence** — Multi-stream convergence point for collecting thoughts from several sources
- **IGridCell\<TIn, TOut\>** — Interface for processing cells that transform thought streams

### Ouroboros.Hypergrid.Mesh

The interconnection mesh layer that wires Ouroboros instances together:

- **OuroborosNode** — Wraps an Ouroboros pipeline as a mesh node with health tracking
- **IInterwire** — Cross-connection protocol between nodes along grid edges
- **StreamConnection** — Active connection record between two nodes
- **MeshOrchestrator** — CLI-driven lifecycle management: register, interwire, monitor, shutdown
- **NodeHealth** — Health status record with heartbeat, processed/error counts

## Grid Dimensions

Each dimension in the hypergrid represents a distinct reasoning axis:

| Dimension | Axis         | Purpose                            |
|-----------|--------------|------------------------------------|
| dim-0     | **Temporal** | Time-ordered thought progression   |
| dim-1     | **Semantic** | Conceptual similarity / topic space|
| dim-2     | **Causal**   | Cause-effect reasoning chains      |
| dim-3     | **Modal**    | Possibility/necessity exploration  |
| dim-N     | **Extensible** | User-defined reasoning axes      |

## Build Position

```
ouroboros-build (Directory.Build.props)
    │
    ├── ouroboros-foundation          ← No upstream deps
    │
    ├── ouroboros-engine              ← Depends on Foundation
    │
    ├── ouroboros-iaret               ← Identity (asset-only)
    │
    └── ouroboros-hypergrid           ← Depends on Foundation + Engine
            │
            └── ouroboros-app         ← Depends on all above
```

## Data Flow

```
  Thought originates          Stream routes through grid          Thoughts converge
  ┌──────────────┐    ┌──────────────────────────────┐    ┌──────────────────┐
  │ ThoughtStream │───▶│ StreamRouter + FlowPolicy    │───▶│ Confluence<T>    │
  │  .Of(thought) │    │  .ResolveTargets(coordinate) │    │  .Emit() / .CollectFirst() │
  └──────────────┘    └──────────────────────────────┘    └──────────────────┘
        │                         │                              │
        ▼                         ▼                              ▼
   IGridCell.Process()     DimensionalProjection          MeshOrchestrator
   at each vertex          .Project() / .Slice()          .GetHealthReport()
```
