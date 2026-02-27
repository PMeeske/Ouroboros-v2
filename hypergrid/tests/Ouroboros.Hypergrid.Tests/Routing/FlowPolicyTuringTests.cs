// <copyright file="FlowPolicyTuringTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Ouroboros.Hypergrid.Tests.Routing;

using FluentAssertions;
using Ouroboros.Hypergrid.Routing;
using Xunit;

/// <summary>
/// Turing tests for FlowPolicy -- validates the routing policy record that governs
/// how thought streams propagate through the Hypergrid. Policy construction, factory
/// methods, record semantics, and strategy/dimension associations must all be correct,
/// since incorrect routing policies would silently misdirect thought streams.
/// </summary>
[Trait("Category", "Unit")]
public sealed class FlowPolicyTuringTests
{
    // -- Default Construction ------------------------------------------------

    [Fact]
    public void Default_strategy_should_be_broadcast()
    {
        var policy = new FlowPolicy();
        policy.Strategy.Should().Be(FlowStrategy.Broadcast);
    }

    [Fact]
    public void Default_preferred_dimension_should_be_zero()
    {
        var policy = new FlowPolicy();
        policy.PreferredDimension.Should().Be(0);
    }

    // -- Factory Methods: Broadcast ------------------------------------------

    [Fact]
    public void Broadcast_factory_should_return_broadcast_strategy()
    {
        var policy = FlowPolicy.Broadcast;

        policy.Strategy.Should().Be(FlowStrategy.Broadcast);
    }

    [Fact]
    public void Broadcast_factory_preferred_dimension_should_be_zero()
    {
        var policy = FlowPolicy.Broadcast;

        policy.PreferredDimension.Should().Be(0);
    }

    // -- Factory Methods: Nearest --------------------------------------------

    [Fact]
    public void Nearest_factory_should_return_nearest_strategy()
    {
        var policy = FlowPolicy.Nearest;

        policy.Strategy.Should().Be(FlowStrategy.Nearest);
    }

    [Fact]
    public void Nearest_factory_preferred_dimension_should_be_zero()
    {
        var policy = FlowPolicy.Nearest;

        policy.PreferredDimension.Should().Be(0);
    }

    // -- Factory Methods: ForDimension ---------------------------------------

    [Fact]
    public void ForDimension_should_set_dimensional_strategy()
    {
        var policy = FlowPolicy.ForDimension(2);

        policy.Strategy.Should().Be(FlowStrategy.Dimensional);
    }

    [Fact]
    public void ForDimension_should_store_preferred_dimension()
    {
        var policy = FlowPolicy.ForDimension(3);

        policy.PreferredDimension.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(99)]
    public void ForDimension_should_accept_various_dimension_indices(int dim)
    {
        var policy = FlowPolicy.ForDimension(dim);

        policy.Strategy.Should().Be(FlowStrategy.Dimensional);
        policy.PreferredDimension.Should().Be(dim);
    }

    // -- Record Equality -----------------------------------------------------

    [Fact]
    public void Identical_broadcast_policies_should_be_equal()
    {
        var a = FlowPolicy.Broadcast;
        var b = FlowPolicy.Broadcast;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Identical_nearest_policies_should_be_equal()
    {
        var a = FlowPolicy.Nearest;
        var b = FlowPolicy.Nearest;

        a.Should().Be(b);
    }

    [Fact]
    public void Identical_dimensional_policies_should_be_equal()
    {
        var a = FlowPolicy.ForDimension(2);
        var b = FlowPolicy.ForDimension(2);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_strategies_should_not_be_equal()
    {
        var broadcast = FlowPolicy.Broadcast;
        var nearest = FlowPolicy.Nearest;

        broadcast.Should().NotBe(nearest);
    }

    [Fact]
    public void Different_preferred_dimensions_should_not_be_equal()
    {
        var dimA = FlowPolicy.ForDimension(0);
        var dimB = FlowPolicy.ForDimension(1);

        dimA.Should().NotBe(dimB);
    }

    [Fact]
    public void Broadcast_and_dimensional_with_default_dimension_should_not_be_equal()
    {
        // Even though PreferredDimension is 0 for both, Strategy differs
        var broadcast = FlowPolicy.Broadcast;
        var dimensional = FlowPolicy.ForDimension(0);

        broadcast.Should().NotBe(dimensional);
    }

    // -- With-Expression (Record Copy) ----------------------------------------

    [Fact]
    public void With_expression_should_create_modified_copy()
    {
        var original = FlowPolicy.Broadcast;
        var modified = original with { Strategy = FlowStrategy.Nearest };

        modified.Strategy.Should().Be(FlowStrategy.Nearest);
        original.Strategy.Should().Be(FlowStrategy.Broadcast, "original should be unchanged");
    }

    [Fact]
    public void With_expression_should_allow_changing_preferred_dimension()
    {
        var original = FlowPolicy.ForDimension(0);
        var modified = original with { PreferredDimension = 5 };

        modified.PreferredDimension.Should().Be(5);
        modified.Strategy.Should().Be(FlowStrategy.Dimensional, "strategy should be preserved");
    }

    // -- FlowStrategy Enum ---------------------------------------------------

    [Fact]
    public void FlowStrategy_should_have_three_values()
    {
        var values = Enum.GetValues<FlowStrategy>();
        values.Should().HaveCount(3);
    }

    [Fact]
    public void FlowStrategy_should_contain_expected_members()
    {
        var values = Enum.GetValues<FlowStrategy>();

        values.Should().Contain(FlowStrategy.Broadcast);
        values.Should().Contain(FlowStrategy.Nearest);
        values.Should().Contain(FlowStrategy.Dimensional);
    }

    [Theory]
    [InlineData(FlowStrategy.Broadcast, 0)]
    [InlineData(FlowStrategy.Nearest, 1)]
    [InlineData(FlowStrategy.Dimensional, 2)]
    public void FlowStrategy_should_have_expected_ordinal_values(FlowStrategy strategy, int expected)
    {
        ((int)strategy).Should().Be(expected);
    }

    // -- Init-Only Properties via Object Initializer -------------------------

    [Fact]
    public void Should_construct_via_object_initializer()
    {
        var policy = new FlowPolicy
        {
            Strategy = FlowStrategy.Dimensional,
            PreferredDimension = 7
        };

        policy.Strategy.Should().Be(FlowStrategy.Dimensional);
        policy.PreferredDimension.Should().Be(7);
    }

    // -- ToString (Record Default) -------------------------------------------

    [Fact]
    public void ToString_should_contain_strategy_info()
    {
        var policy = FlowPolicy.ForDimension(2);
        var str = policy.ToString();

        str.Should().NotBeNullOrWhiteSpace();
        str.Should().Contain("FlowPolicy");
        str.Should().Contain("Dimensional");
    }

    // -- Determinism ---------------------------------------------------------

    [Fact]
    public void Factory_methods_should_produce_consistent_results()
    {
        // Calling factory methods multiple times should yield equal policies
        FlowPolicy.Broadcast.Should().Be(FlowPolicy.Broadcast);
        FlowPolicy.Nearest.Should().Be(FlowPolicy.Nearest);
        FlowPolicy.ForDimension(3).Should().Be(FlowPolicy.ForDimension(3));
    }
}
