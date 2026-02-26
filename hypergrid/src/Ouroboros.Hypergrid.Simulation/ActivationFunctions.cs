namespace Ouroboros.Hypergrid.Simulation;

/// <summary>Delegate for activation functions used in CPU simulation.</summary>
public delegate double ActivationFunction(double input);

/// <summary>
/// Standard activation functions for thought propagation simulation.
/// These mirror the functions used in the GPU kernels.
/// </summary>
public static class ActivationFunctions
{
    /// <summary>Hyperbolic tangent — squashes to [-1, 1]. Default for thought propagation.</summary>
    public static double Tanh(double x) => Math.Tanh(x);

    /// <summary>Sigmoid — squashes to [0, 1]. Useful for positive-only activation.</summary>
    public static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

    /// <summary>ReLU — max(0, x). Fast, sparse activation.</summary>
    public static double ReLU(double x) => Math.Max(0, x);

    /// <summary>Identity — no transformation. For linear propagation.</summary>
    public static double Identity(double x) => x;

    /// <summary>
    /// Soft convergence — a dampened identity that naturally converges.
    /// f(x) = x * decay, where decay &lt; 1.
    /// </summary>
    public static ActivationFunction SoftConvergence(double decay = 0.9) =>
        x => x * decay;
}
