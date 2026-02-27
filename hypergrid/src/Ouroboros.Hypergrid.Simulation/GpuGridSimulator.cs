namespace Ouroboros.Hypergrid.Simulation;

using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;

/// <summary>
/// GPU-accelerated grid simulator using ILGPU's OpenCL backend.
/// Each cell's activation update runs as a parallel work-item on the GPU.
/// Falls back to CPU accelerator if no OpenCL device is available.
///
/// The propagation kernel computes, for each cell i:
///   activation'[i] = tanh( Σ(weight[e] * activation[source[e]]) )
/// where the sum is over all incoming edges to cell i.
/// </summary>
public sealed class GpuGridSimulator : IGridSimulator
{
    private readonly Context _context;
    private readonly Accelerator _accelerator;
    private readonly Action<Index1D, ArrayView<double>, ArrayView<int>, ArrayView<int>, ArrayView<double>, ArrayView<double>> _kernel;
    private readonly bool _ownsContext;

    public string BackendName { get; }

    /// <summary>
    /// Creates a GPU simulator, preferring OpenCL devices.
    /// Falls back to CPU accelerator if no GPU is available.
    /// </summary>
    public GpuGridSimulator()
    {
        _context = Context.Create(b => b.Default().OpenCL());
        _ownsContext = true;

        // Prefer OpenCL GPU, fall back to CPU
        var device = _context.GetPreferredDevice(preferCPU: false);
        _accelerator = device.CreateAccelerator(_context);
        BackendName = _accelerator is CLAccelerator ? "OpenCL" : "CPU (ILGPU)";

        _kernel = _accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView<double>, ArrayView<int>, ArrayView<int>, ArrayView<double>, ArrayView<double>>(
            PropagationKernel);
    }

    /// <summary>
    /// Creates a GPU simulator using an existing ILGPU accelerator (for testing).
    /// </summary>
    public GpuGridSimulator(Accelerator accelerator)
    {
        ArgumentNullException.ThrowIfNull(accelerator);
        _context = accelerator.Context;
        _accelerator = accelerator;
        _ownsContext = false;
        BackendName = _accelerator is CLAccelerator ? "OpenCL" : "CPU (ILGPU)";

        _kernel = _accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView<double>, ArrayView<int>, ArrayView<int>, ArrayView<double>, ArrayView<double>>(
            PropagationKernel);
    }

    /// <summary>
    /// ILGPU kernel: runs one work-item per cell.
    /// Reads incoming neighbor activations, computes weighted sum, applies tanh.
    /// </summary>
    private static void PropagationKernel(
        Index1D cellIndex,
        ArrayView<double> activations,
        ArrayView<int> edgeRowPtr,
        ArrayView<int> edgeTargets,
        ArrayView<double> edgeWeights,
        ArrayView<double> output)
    {
        var i = cellIndex.X;
        var start = edgeRowPtr[i];
        var end = edgeRowPtr[i + 1];

        if (start == end)
        {
            // No incoming edges — retain current activation
            output[i] = activations[i];
            return;
        }

        var sum = 0.0;
        for (var e = start; e < end; e++)
        {
            var srcIdx = edgeTargets[e];
            sum += activations[srcIdx] * edgeWeights[e];
        }

        output[i] = XMath.Tanh((float)sum);
    }

    public SimulationState Step(SimulationState state)
    {
        var n = state.CellCount;

        using var activBuf = _accelerator.Allocate1D(state.Activations);
        using var rowPtrBuf = _accelerator.Allocate1D(state.EdgeRowPtr);
        using var targetsBuf = _accelerator.Allocate1D(state.EdgeTargets);
        using var weightsBuf = _accelerator.Allocate1D(state.EdgeWeights);
        using var outputBuf = _accelerator.Allocate1D<double>(n);

        _kernel(n, activBuf.View, rowPtrBuf.View, targetsBuf.View, weightsBuf.View, outputBuf.View);
        _accelerator.Synchronize();

        var newActivations = outputBuf.GetAsArray1D();
        return state.WithActivations(newActivations, state.StepNumber + 1);
    }

    public (SimulationState FinalState, int Steps) RunUntilConvergence(
        SimulationState initial,
        double convergenceThreshold = 1e-6,
        int maxSteps = 1000)
    {
        var current = initial;

        for (var step = 0; step < maxSteps; step++)
        {
            var next = Step(current);

            if (next.MaxDelta(current) < convergenceThreshold)
                return (next, step + 1);

            current = next;
        }

        return (current, maxSteps);
    }

    public void Dispose()
    {
        _accelerator.Dispose();
        if (_ownsContext)
            _context.Dispose();
    }
}
