// <copyright file="ActivationFunctionsTuringTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Ouroboros.Hypergrid.Tests.Simulation;

using FluentAssertions;
using Ouroboros.Hypergrid.Simulation;
using Xunit;

/// <summary>
/// Turing tests for ActivationFunctions -- validates that all standard activation
/// functions produce mathematically correct outputs, respect their documented ranges,
/// and exhibit the expected boundary behavior. These functions drive thought propagation
/// in the Hypergrid simulation, so numerical fidelity is critical.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ActivationFunctionsTuringTests
{
    // -- Tanh ----------------------------------------------------------------

    [Fact]
    public void Tanh_zero_should_return_zero()
    {
        ActivationFunctions.Tanh(0.0).Should().Be(0.0);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(100.0)]
    public void Tanh_positive_input_should_return_value_in_zero_to_one(double x)
    {
        var result = ActivationFunctions.Tanh(x);
        result.Should().BeGreaterThan(0.0);
        result.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Tanh_large_positive_should_saturate_near_one()
    {
        ActivationFunctions.Tanh(100.0).Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Tanh_large_negative_should_saturate_near_negative_one()
    {
        ActivationFunctions.Tanh(-100.0).Should().BeApproximately(-1.0, 1e-10);
    }

    [Fact]
    public void Tanh_should_be_odd_function()
    {
        // tanh(-x) = -tanh(x)
        var x = 1.5;
        ActivationFunctions.Tanh(-x).Should().BeApproximately(-ActivationFunctions.Tanh(x), 1e-15);
    }

    [Fact]
    public void Tanh_known_value()
    {
        // tanh(1) ~= 0.7615941559557649
        ActivationFunctions.Tanh(1.0).Should().BeApproximately(0.7615941559557649, 1e-12);
    }

    // -- Sigmoid -------------------------------------------------------------

    [Fact]
    public void Sigmoid_zero_should_return_half()
    {
        ActivationFunctions.Sigmoid(0.0).Should().Be(0.5);
    }

    [Fact]
    public void Sigmoid_large_positive_should_approach_one()
    {
        ActivationFunctions.Sigmoid(100.0).Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Sigmoid_large_negative_should_approach_zero()
    {
        ActivationFunctions.Sigmoid(-100.0).Should().BeApproximately(0.0, 1e-10);
    }

    [Theory]
    [InlineData(-50.0)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(50.0)]
    public void Sigmoid_should_always_return_value_in_zero_to_one(double x)
    {
        var result = ActivationFunctions.Sigmoid(x);
        result.Should().BeGreaterThanOrEqualTo(0.0);
        result.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Sigmoid_symmetry_around_zero()
    {
        // sigmoid(x) + sigmoid(-x) = 1
        var x = 2.0;
        var sum = ActivationFunctions.Sigmoid(x) + ActivationFunctions.Sigmoid(-x);
        sum.Should().BeApproximately(1.0, 1e-15);
    }

    [Fact]
    public void Sigmoid_known_value()
    {
        // sigmoid(1) = 1 / (1 + e^-1) ~= 0.7310585786300049
        ActivationFunctions.Sigmoid(1.0).Should().BeApproximately(0.7310585786300049, 1e-12);
    }

    // -- ReLU ----------------------------------------------------------------

    [Fact]
    public void ReLU_positive_should_pass_through()
    {
        ActivationFunctions.ReLU(5.0).Should().Be(5.0);
    }

    [Fact]
    public void ReLU_zero_should_return_zero()
    {
        ActivationFunctions.ReLU(0.0).Should().Be(0.0);
    }

    [Fact]
    public void ReLU_negative_should_return_zero()
    {
        ActivationFunctions.ReLU(-3.0).Should().Be(0.0);
    }

    [Theory]
    [InlineData(-100.0, 0.0)]
    [InlineData(-0.001, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.001, 0.001)]
    [InlineData(42.0, 42.0)]
    public void ReLU_should_compute_max_of_zero_and_input(double input, double expected)
    {
        ActivationFunctions.ReLU(input).Should().Be(expected);
    }

    // -- Identity ------------------------------------------------------------

    [Theory]
    [InlineData(-999.0)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(999.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void Identity_should_return_input_unchanged(double x)
    {
        ActivationFunctions.Identity(x).Should().Be(x);
    }

    // -- SoftConvergence -----------------------------------------------------

    [Fact]
    public void SoftConvergence_default_decay_should_be_0_9()
    {
        var fn = ActivationFunctions.SoftConvergence();
        fn(10.0).Should().BeApproximately(9.0, 1e-15, "10.0 * 0.9 = 9.0");
    }

    [Fact]
    public void SoftConvergence_custom_decay()
    {
        var fn = ActivationFunctions.SoftConvergence(0.5);
        fn(8.0).Should().BeApproximately(4.0, 1e-15, "8.0 * 0.5 = 4.0");
    }

    [Fact]
    public void SoftConvergence_zero_input_should_remain_zero()
    {
        var fn = ActivationFunctions.SoftConvergence(0.9);
        fn(0.0).Should().Be(0.0);
    }

    [Fact]
    public void SoftConvergence_repeated_application_should_converge_toward_zero()
    {
        var fn = ActivationFunctions.SoftConvergence(0.9);
        var value = 100.0;

        for (var i = 0; i < 200; i++)
            value = fn(value);

        value.Should().BeApproximately(0.0, 1e-6, "repeated decay should converge toward zero");
    }

    [Fact]
    public void SoftConvergence_decay_of_one_should_act_as_identity()
    {
        var fn = ActivationFunctions.SoftConvergence(1.0);
        fn(42.0).Should().Be(42.0);
    }

    [Fact]
    public void SoftConvergence_decay_of_zero_should_always_return_zero()
    {
        var fn = ActivationFunctions.SoftConvergence(0.0);
        fn(42.0).Should().Be(0.0);
    }

    // -- Delegate Compatibility ----------------------------------------------

    [Fact]
    public void All_functions_should_be_assignable_to_ActivationFunction_delegate()
    {
        ActivationFunction tanh = ActivationFunctions.Tanh;
        ActivationFunction sigmoid = ActivationFunctions.Sigmoid;
        ActivationFunction relu = ActivationFunctions.ReLU;
        ActivationFunction identity = ActivationFunctions.Identity;
        ActivationFunction soft = ActivationFunctions.SoftConvergence(0.8);

        // Verify they all execute without error
        tanh(1.0).Should().BeGreaterThan(0.0);
        sigmoid(1.0).Should().BeGreaterThan(0.0);
        relu(1.0).Should().Be(1.0);
        identity(1.0).Should().Be(1.0);
        soft(1.0).Should().BeApproximately(0.8, 1e-15);
    }

    // -- Special Floating Point Values ---------------------------------------

    [Fact]
    public void Tanh_of_NaN_should_return_NaN()
    {
        double.IsNaN(ActivationFunctions.Tanh(double.NaN)).Should().BeTrue();
    }

    [Fact]
    public void Sigmoid_of_positive_infinity_should_return_one()
    {
        ActivationFunctions.Sigmoid(double.PositiveInfinity).Should().Be(1.0);
    }

    [Fact]
    public void Sigmoid_of_negative_infinity_should_return_zero()
    {
        ActivationFunctions.Sigmoid(double.NegativeInfinity).Should().Be(0.0);
    }

    [Fact]
    public void ReLU_of_positive_infinity_should_return_positive_infinity()
    {
        ActivationFunctions.ReLU(double.PositiveInfinity).Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void ReLU_of_negative_infinity_should_return_zero()
    {
        ActivationFunctions.ReLU(double.NegativeInfinity).Should().Be(0.0);
    }
}
