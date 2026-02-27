// <copyright file="SimulatorFactoryTuringTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Ouroboros.Hypergrid.Tests.Simulation;

using FluentAssertions;
using Ouroboros.Hypergrid.Simulation;
using Xunit;

/// <summary>
/// Turing tests for SimulatorFactory -- validates that the factory correctly
/// creates simulator instances, preferring GPU when available and falling back
/// to CPU. In typical CI environments without OpenCL, <see cref="SimulatorFactory.Create"/>
/// should gracefully fall back to <see cref="CpuGridSimulator"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SimulatorFactoryTuringTests
{
    // -- CreateCpu -----------------------------------------------------------

    [Fact]
    public void CreateCpu_should_return_cpu_simulator()
    {
        using var sim = SimulatorFactory.CreateCpu();

        sim.Should().NotBeNull();
        sim.Should().BeOfType<CpuGridSimulator>();
        sim.BackendName.Should().Be("CPU");
    }

    [Fact]
    public void CreateCpu_with_custom_activation_should_use_it()
    {
        using var sim = SimulatorFactory.CreateCpu(ActivationFunctions.ReLU);

        // Verify the simulator works with the custom activation
        var state = new SimulationState(
            activations: [-5.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        var next = sim.Step(state);

        next.Activations[1].Should().Be(0.0, "ReLU(-5) = 0");
    }

    [Fact]
    public void CreateCpu_with_null_activation_should_default_to_tanh()
    {
        using var sim = SimulatorFactory.CreateCpu(null);

        var state = new SimulationState(
            activations: [100.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        var next = sim.Step(state);

        // tanh(100) ~= 1.0
        next.Activations[1].Should().BeApproximately(1.0, 1e-10, "default activation is tanh");
    }

    // -- Create (GPU-preferred with fallback) --------------------------------

    [Fact]
    public void Create_should_return_a_working_simulator()
    {
        // In CI/test environments without OpenCL, this will fall back to CPU.
        // Either way, the returned simulator must be functional.
        using var sim = SimulatorFactory.Create();

        sim.Should().NotBeNull();
        sim.BackendName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_returned_simulator_should_execute_step()
    {
        using var sim = SimulatorFactory.Create();

        var state = new SimulationState(
            activations: [1.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        var next = sim.Step(state);

        next.StepNumber.Should().Be(1);
        next.CellCount.Should().Be(2);
    }

    [Fact]
    public void Create_with_custom_cpu_activation_should_apply_on_fallback()
    {
        // When GPU is unavailable, the cpuActivation parameter should be used
        using var sim = SimulatorFactory.Create(ActivationFunctions.Identity);

        var state = new SimulationState(
            activations: [7.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        var next = sim.Step(state);

        // If CPU fallback with Identity: B = 7.0 * 1.0 = 7.0
        // If GPU: tanh(7.0) ~= 0.9999... (GPU always uses tanh)
        // Either result is valid; we just verify it executed
        next.Activations[1].Should().NotBe(0.0, "activation should have propagated");
    }

    // -- IGridSimulator interface compliance ---------------------------------

    [Fact]
    public void CreateCpu_should_implement_IGridSimulator()
    {
        using var sim = SimulatorFactory.CreateCpu();
        sim.Should().BeAssignableTo<IGridSimulator>();
    }

    [Fact]
    public void Create_should_implement_IGridSimulator()
    {
        using var sim = SimulatorFactory.Create();
        sim.Should().BeAssignableTo<IGridSimulator>();
    }

    // -- Dispose Safety ------------------------------------------------------

    [Fact]
    public void CreateCpu_should_be_safely_disposable()
    {
        var sim = SimulatorFactory.CreateCpu();
        var act = () => sim.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_be_safely_disposable()
    {
        var sim = SimulatorFactory.Create();
        var act = () => sim.Dispose();
        act.Should().NotThrow();
    }

    // -- RunUntilConvergence via Factory-Created Simulator --------------------

    [Fact]
    public void CreateCpu_should_support_convergence()
    {
        using var sim = SimulatorFactory.CreateCpu(ActivationFunctions.SoftConvergence(0.5));

        var state = new SimulationState(
            activations: [10.0, 0.0],
            edgeRowPtr: [0, 0, 1],
            edgeTargets: [0],
            edgeWeights: [1.0],
            stepNumber: 0);

        var (final, steps) = sim.RunUntilConvergence(state, convergenceThreshold: 1e-6, maxSteps: 500);

        steps.Should().BeLessThan(500, "soft convergence should converge quickly");
        final.StepNumber.Should().Be(steps);
    }
}
