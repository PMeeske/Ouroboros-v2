namespace Ouroboros.Hypergrid.Simulation;

/// <summary>
/// Creates the best available grid simulator: GPU (OpenCL) if a device is present,
/// CPU fallback otherwise. Use this as the default entry point.
/// </summary>
public static class SimulatorFactory
{
    /// <summary>
    /// Creates a simulator, preferring GPU. Returns a CPU simulator if
    /// no OpenCL device is available or if GPU initialization fails.
    /// </summary>
    public static IGridSimulator Create(ActivationFunction? cpuActivation = null)
    {
        try
        {
            var gpu = new GpuGridSimulator();
            return gpu;
        }
        catch
        {
            return new CpuGridSimulator(cpuActivation);
        }
    }

    /// <summary>Forces creation of a CPU-only simulator.</summary>
    public static IGridSimulator CreateCpu(ActivationFunction? activation = null) =>
        new CpuGridSimulator(activation);
}
