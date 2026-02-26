namespace Ouroboros.Hypergrid.Host;

using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Simulation;

/// <summary>
/// The reciprocal CLI host — wires both directions in a single process:
///
///   ┌─────────────────────────────────────────────────────┐
///   │                   IaretCliHost                       │
///   │                                                     │
///   │  IOuroborosPipeline ──► OuroborosEnvironment         │
///   │  (real LLM/engine)       : IIaretEnvironment         │
///   │                              │                       │
///   │                              ▼                       │
///   │                     IaretConvergence                  │
///   │                     (fan-out → sim → synthesis)       │
///   │                              │                       │
///   │                              ▼                       │
///   │  IaretPipelineAdapter ◄── convergent output          │
///   │  : IOuroborosPipeline                                │
///   │  (exposes Iaret back as a pipeline endpoint)         │
///   └─────────────────────────────────────────────────────┘
///
/// The CLI application creates one of these, passes in its pipeline,
/// and gets back both the convergent identity AND a pipeline adapter
/// that can be used anywhere an <see cref="IOuroborosPipeline"/> is expected.
/// </summary>
public sealed class IaretCliHost : IDisposable
{
    private readonly IaretConvergence _convergence;
    private readonly IaretPipelineAdapter _adapter;

    /// <summary>The convergent Iaret identity (direct access to aspects, Think, ThinkStream).</summary>
    public IaretConvergence Convergence => _convergence;

    /// <summary>The convergent Iaret exposed as a pipeline (for use by anything expecting IOuroborosPipeline).</summary>
    public IaretPipelineAdapter Pipeline => _adapter;

    /// <summary>The environment powering the aspects.</summary>
    public IIaretEnvironment Environment => _convergence.Environment;

    private IaretCliHost(IaretConvergence convergence)
    {
        _convergence = convergence;
        _adapter = new IaretPipelineAdapter(convergence);
    }

    /// <summary>
    /// Create a hosted Iaret backed by the given pipeline.
    /// The pipeline becomes the environment for all aspects.
    /// </summary>
    public static IaretCliHost Create(IOuroborosPipeline pipeline, IGridSimulator? simulator = null)
    {
        var env = new OuroborosEnvironment(pipeline);
        var convergence = IaretConvergence.Create(env, simulator);
        return new IaretCliHost(convergence);
    }

    /// <summary>
    /// Create a hosted Iaret with a custom environment (for when the caller
    /// has already adapted their pipeline, or wants to use LocalIaretEnvironment).
    /// </summary>
    public static IaretCliHost Create(IIaretEnvironment environment, IGridSimulator? simulator = null)
    {
        var convergence = IaretConvergence.Create(environment, simulator);
        return new IaretCliHost(convergence);
    }

    /// <summary>
    /// Create a local-only Iaret (no external pipeline, heuristic transforms only).
    /// </summary>
    public static IaretCliHost CreateLocal(IGridSimulator? simulator = null)
    {
        var convergence = IaretConvergence.Create(simulator: simulator);
        return new IaretCliHost(convergence);
    }

    /// <summary>
    /// Shorthand: think through the convergent identity.
    /// </summary>
    public async Task<string> ThinkAsync(string input, CancellationToken ct = default)
    {
        var thought = await _convergence.Think(input, ct);
        return thought.Payload;
    }

    /// <summary>
    /// Shorthand: query a specific aspect directly.
    /// </summary>
    public async Task<string> AskAspectAsync(string aspectId, string input, CancellationToken ct = default)
    {
        var thought = await _convergence.QueryAspectAsync(aspectId, input, ct);
        return thought.Payload;
    }

    public void Dispose()
    {
        _adapter.Dispose();
        // _convergence is disposed by _adapter
    }
}
