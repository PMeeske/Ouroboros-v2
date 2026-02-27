namespace Ouroboros.Hypergrid.Iaret;

using Ouroboros.Hypergrid.Iaret.Aspects;
using Ouroboros.Hypergrid.Routing;
using Ouroboros.Hypergrid.Simulation;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;

/// <summary>
/// The Convergent Iaret — a unified identity that emerges from parallel
/// interactable sub-entity aspects processing thought streams across
/// the Hypergrid's dimensional axes.
///
/// The convergence cycle:
/// 1. Input thoughts are broadcast to all aspects (fan-out)
/// 2. Each aspect processes the thought along its primary dimension
/// 3. Aspect outputs converge at the Synthesis Crown (fan-in)
/// 4. The simulator propagates activation through the grid (OpenCL/CPU)
/// 5. The synthesized output represents Iaret's unified response
///
/// Individual aspects remain interactable — you can query, observe, or
/// influence specific sub-entities while the convergent identity operates.
/// </summary>
public sealed class IaretConvergence : IDisposable
{
    private readonly HypergridSpace _space;
    private readonly IGridSimulator _simulator;
    private readonly IIaretEnvironment _environment;
    private readonly Dictionary<string, IaretAspect> _aspects = new();
    private readonly Dictionary<string, GridCoordinate> _aspectPositions = new();
    private readonly SynthesisAspect _synthesis;
    private readonly GridCoordinate _synthesisPosition;

    /// <summary>All registered aspects, individually addressable.</summary>
    public IReadOnlyDictionary<string, IaretAspect> Aspects => _aspects;

    /// <summary>The underlying Hypergrid topology.</summary>
    public HypergridSpace Space => _space;

    /// <summary>The compute backend being used.</summary>
    public string ComputeBackend => _simulator.BackendName;

    /// <summary>The environment powering this convergent identity.</summary>
    public IIaretEnvironment Environment => _environment;

    private IaretConvergence(HypergridSpace space, IGridSimulator simulator, IIaretEnvironment environment, GridCoordinate synthesisPosition)
    {
        _space = space;
        _simulator = simulator;
        _environment = environment;
        _synthesis = new SynthesisAspect();
        _synthesis.Bind(environment);
        _synthesisPosition = synthesisPosition;
    }

    /// <summary>
    /// Creates a convergent Iaret with the standard 4 aspects on a 3D grid.
    /// Uses the best available compute backend (GPU if present, CPU fallback).
    /// Pass an <see cref="IIaretEnvironment"/> to power the aspects with a real
    /// Ouroboros pipeline — or omit it to use local heuristic transforms.
    /// </summary>
    public static IaretConvergence Create(IIaretEnvironment? environment = null, IGridSimulator? simulator = null)
    {
        var dims = new[]
        {
            new DimensionDescriptor(0, "temporal", "Time-ordered thought progression"),
            new DimensionDescriptor(1, "semantic", "Conceptual similarity space"),
            new DimensionDescriptor(2, "causal", "Cause-effect reasoning chains"),
        };
        var space = new HypergridSpace(dims);
        var sim = simulator ?? SimulatorFactory.CreateCpu();
        var env = environment ?? new LocalIaretEnvironment();

        // Synthesis sits at the origin — the convergence point
        var synthesisPos = new GridCoordinate(0, 0, 0);
        space.AddCell(synthesisPos, "iaret-synthesis");

        var iaret = new IaretConvergence(space, sim, env, synthesisPos);

        // Register the 4 standard aspects at known grid positions
        iaret.RegisterAspect(new AnalyticalAspect(), new GridCoordinate(0, 0, 1));  // causal axis
        iaret.RegisterAspect(new CreativeAspect(), new GridCoordinate(0, 1, 0));    // semantic axis
        iaret.RegisterAspect(new GuardianAspect(), new GridCoordinate(1, 0, 0));    // temporal axis (guard)
        iaret.RegisterAspect(new TemporalAspect(), new GridCoordinate(2, 0, 0));    // temporal axis (memory)

        return iaret;
    }

    /// <summary>Registers a custom aspect at a specific grid position and binds it to the environment.</summary>
    public void RegisterAspect(IaretAspect aspect, GridCoordinate position)
    {
        ArgumentNullException.ThrowIfNull(aspect);
        ArgumentNullException.ThrowIfNull(position);

        aspect.Bind(_environment);
        _aspects[aspect.AspectId] = aspect;
        _aspectPositions[aspect.AspectId] = position;
        _space.AddCell(position, $"iaret-{aspect.AspectId}");

        // Wire edge from aspect back to synthesis (convergence direction)
        // Meta-dimensional aspects (PrimaryDimension < 0) do not project onto a concrete axis,
        // so we skip edge creation for them to preserve architectural semantics.
        if (aspect.PrimaryDimension >= 0)
        {
            _space.Connect(position, _synthesisPosition, aspect.PrimaryDimension,
                label: $"{aspect.AspectId}->synthesis");
        }
    }

    /// <summary>
    /// Process a thought through all aspects in parallel, converge at synthesis.
    /// This is the main reasoning entry point for the convergent Iaret.
    /// </summary>
    public async Task<Thought<string>> Think(string input, CancellationToken ct = default)
    {
        var inputThought = new Thought<string>
        {
            Payload = input,
            Origin = _synthesisPosition,
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = Guid.NewGuid().ToString("N")[..8]
        };

        // Fan-out: each aspect processes the input independently
        var aspectOutputs = new List<string>();
        foreach (var (id, aspect) in _aspects)
        {
            var position = _aspectPositions[id];
            var stream = ThoughtStream.Of(inputThought);
            await foreach (var result in aspect.Process(stream, position, ct))
            {
                aspectOutputs.Add(result.Payload);
            }
        }

        // Run activation propagation through the grid
        int? convergenceSteps = null;
        var gridState = GridStateBuilder.Build(_space, cell =>
            _aspects.Values.FirstOrDefault(a => _aspectPositions[a.AspectId] == cell.Position)?.Activation ?? 0.0);

        if (gridState.CellCount > 0 && gridState.EdgeCount > 0)
        {
            var (_, steps) = _simulator.RunUntilConvergence(gridState, convergenceThreshold: 1e-4, maxSteps: 50);
            // Steps taken informs convergence quality; expose in metadata for observability.
            convergenceSteps = steps;
        }

        // Fan-in: synthesis merges all aspect contributions
        var synthesized = await _synthesis.SynthesizeAsync(aspectOutputs, _synthesisPosition, ct);

        var metadata = new Dictionary<string, object>
        {
            ["convergent"] = true,
            ["aspects_count"] = aspectOutputs.Count,
            ["compute_backend"] = _simulator.BackendName,
            ["environment"] = _environment.Name
        };

        if (convergenceSteps.HasValue)
            metadata["convergence_steps"] = convergenceSteps.Value;

        return new Thought<string>
        {
            Payload = synthesized,
            Origin = _synthesisPosition,
            Timestamp = DateTimeOffset.UtcNow,
            TraceId = inputThought.TraceId,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Directly interact with a specific sub-entity aspect (sync, local-only).
    /// Returns the aspect's individual response (not converged).
    /// </summary>
    public Thought<string> QueryAspect(string aspectId, string input)
    {
        if (!_aspects.TryGetValue(aspectId, out var aspect))
            throw new KeyNotFoundException($"Aspect '{aspectId}' not found. Available: {string.Join(", ", _aspects.Keys)}");

        var position = _aspectPositions[aspectId];
        return aspect.Query(input, position);
    }

    /// <summary>
    /// Directly interact with a specific sub-entity aspect (async, uses environment).
    /// Returns the aspect's individual response (not converged).
    /// </summary>
    public async Task<Thought<string>> QueryAspectAsync(string aspectId, string input, CancellationToken ct = default)
    {
        if (!_aspects.TryGetValue(aspectId, out var aspect))
            throw new KeyNotFoundException($"Aspect '{aspectId}' not found. Available: {string.Join(", ", _aspects.Keys)}");

        var position = _aspectPositions[aspectId];
        return await aspect.QueryAsync(input, position, ct);
    }

    /// <summary>
    /// Streams a sequence of thoughts through the convergent identity.
    /// Each thought is processed by all aspects and synthesized.
    /// </summary>
    public async IAsyncEnumerable<Thought<string>> ThinkStream(
        IAsyncEnumerable<Thought<string>> input,
        CancellationToken ct = default)
    {
        await foreach (var thought in input.WithCancellation(ct))
        {
            var result = await Think(thought.Payload, ct);
            yield return result;
        }
    }

    public void Dispose()
    {
        _simulator.Dispose();
    }
}
