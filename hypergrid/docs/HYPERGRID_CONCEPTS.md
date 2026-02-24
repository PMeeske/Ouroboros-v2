# Hypergrid Concepts

## Dimensional Thinking

The Hypergrid models reasoning as movement through an N-dimensional space. Rather than a linear chain of thoughts, ideas can propagate along multiple axes simultaneously:

### Temporal (dim-0)
The most natural dimension — time. Thoughts flow forward in sequence, each building on the previous. This is the familiar "chain of thought" pattern, mapped to the grid's first axis.

### Semantic (dim-1)
Conceptual proximity. Adjacent cells along the semantic axis process related topics. A thought about "monads" sits near "functors" and "applicatives", enabling lateral reasoning jumps.

### Causal (dim-2)
Cause-and-effect chains. Moving along the causal axis traces *why* something is true rather than *what* is related. This enables root-cause analysis and counterfactual reasoning.

### Modal (dim-3)
Possibility and necessity. The modal axis explores what *could* be true vs. what *must* be true. Each step along this axis represents an alternative scenario or constraint relaxation.

### Extensible (dim-N)
User-defined axes for domain-specific reasoning. Examples:
- **Confidence axis** — gradient from speculative to certain
- **Abstraction axis** — concrete implementation to abstract principle
- **Stakeholder axis** — different perspectives on the same problem

## Stream Semantics

### Thought as Data
A `Thought<T>` is an immutable record carrying:
- **Payload** — the typed reasoning content
- **Origin** — the GridCoordinate where it was produced
- **Timestamp** — when it was produced
- **TraceId** — for distributed tracing across the mesh
- **Metadata** — extensible key-value context

### Stream Composition
Thought streams are `IAsyncEnumerable<Thought<T>>` and compose naturally:

```
stream1 ──┐
           ├── Merge ──▶ Select(transform) ──▶ Where(filter) ──▶ output
stream2 ──┘
```

### Confluence
A Confluence is a named convergence point where multiple streams meet. It supports:
- **Emit** — interleave all sources, emitting thoughts as they arrive
- **CollectFirst** — wait for one thought from each source, then batch

## Interwiring

Interwiring is the cross-connection protocol between Ouroboros nodes. When two nodes are interwired along an edge:

1. The source node's output thought stream is forwarded to the target
2. The edge's dimension metadata tells the target *how* to interpret incoming thoughts
3. Back-pressure propagates naturally through `IAsyncEnumerable`

## Mesh Orchestration

The MeshOrchestrator manages the mesh lifecycle from the CLI:

1. **Register** — Add a node at a grid coordinate
2. **Interwire** — Connect nodes along dimensions
3. **Monitor** — Poll health reports (heartbeat, processed/error counts)
4. **Shutdown** — Gracefully disconnect and drain streams

```
CLI ──▶ MeshOrchestrator.Register("Ou-1", (0,0,0))
CLI ──▶ MeshOrchestrator.Register("Ou-2", (1,0,0))
CLI ──▶ MeshOrchestrator.Interwire("Ou-1", "Ou-2", dimension: 1)
CLI ──▶ MeshOrchestrator.GetHealthReport()
```
