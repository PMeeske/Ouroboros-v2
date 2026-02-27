// <copyright file="GridEdgeTuringTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Ouroboros.Hypergrid.Tests.Topology;

using FluentAssertions;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for GridEdge -- validates the directed edge record that connects
/// two vertices in the Hypergrid. Edges carry dimensional metadata and weight,
/// forming the wiring that determines how thought streams propagate. Record
/// semantics (equality, immutability, with-expressions) must be correct.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GridEdgeTuringTests
{
    // -- Construction --------------------------------------------------------

    [Fact]
    public void Should_create_edge_with_required_properties()
    {
        var source = new GridCoordinate(0, 0);
        var target = new GridCoordinate(1, 0);

        var edge = new GridEdge(source, target, Dimension: 0);

        edge.Source.Should().Be(source);
        edge.Target.Should().Be(target);
        edge.Dimension.Should().Be(0);
    }

    [Fact]
    public void Label_should_default_to_null()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), Dimension: 0);

        edge.Label.Should().BeNull();
    }

    [Fact]
    public void Should_create_edge_with_label()
    {
        var edge = new GridEdge(
            new GridCoordinate(0, 0),
            new GridCoordinate(1, 0),
            Dimension: 0,
            Label: "temporal-link");

        edge.Label.Should().Be("temporal-link");
    }

    // -- Weight --------------------------------------------------------------

    [Fact]
    public void Weight_should_default_to_one()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0);

        edge.Weight.Should().Be(1.0);
    }

    [Fact]
    public void Should_create_edge_with_custom_weight()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0)
        {
            Weight = 0.75
        };

        edge.Weight.Should().Be(0.75);
    }

    [Fact]
    public void Should_support_zero_weight()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0)
        {
            Weight = 0.0
        };

        edge.Weight.Should().Be(0.0);
    }

    [Fact]
    public void Should_support_negative_weight()
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0)
        {
            Weight = -0.5
        };

        edge.Weight.Should().Be(-0.5, "negative weights represent inhibitory connections");
    }

    // -- Record Equality -----------------------------------------------------

    [Fact]
    public void Identical_edges_should_be_equal()
    {
        var source = new GridCoordinate(0, 0);
        var target = new GridCoordinate(1, 0);

        var a = new GridEdge(source, target, 0, "link");
        var b = new GridEdge(source, target, 0, "link");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Edges_with_different_source_should_not_be_equal()
    {
        var target = new GridCoordinate(1, 0);
        var a = new GridEdge(new GridCoordinate(0, 0), target, 0);
        var b = new GridEdge(new GridCoordinate(2, 0), target, 0);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Edges_with_different_target_should_not_be_equal()
    {
        var source = new GridCoordinate(0, 0);
        var a = new GridEdge(source, new GridCoordinate(1, 0), 0);
        var b = new GridEdge(source, new GridCoordinate(2, 0), 0);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Edges_with_different_dimension_should_not_be_equal()
    {
        var source = new GridCoordinate(0, 0);
        var target = new GridCoordinate(1, 0);

        var a = new GridEdge(source, target, 0);
        var b = new GridEdge(source, target, 1);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Edges_with_different_label_should_not_be_equal()
    {
        var source = new GridCoordinate(0, 0);
        var target = new GridCoordinate(1, 0);

        var a = new GridEdge(source, target, 0, "alpha");
        var b = new GridEdge(source, target, 0, "beta");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Edges_with_different_weight_should_not_be_equal()
    {
        var source = new GridCoordinate(0, 0);
        var target = new GridCoordinate(1, 0);

        var a = new GridEdge(source, target, 0) { Weight = 1.0 };
        var b = new GridEdge(source, target, 0) { Weight = 0.5 };

        a.Should().NotBe(b);
    }

    // -- With-Expression (Record Copy) ----------------------------------------

    [Fact]
    public void With_expression_should_create_modified_copy()
    {
        var original = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0, "original")
        {
            Weight = 1.0
        };

        var modified = original with { Weight = 0.3 };

        modified.Weight.Should().Be(0.3);
        modified.Source.Should().Be(original.Source);
        modified.Target.Should().Be(original.Target);
        modified.Dimension.Should().Be(original.Dimension);
        modified.Label.Should().Be("original");
    }

    [Fact]
    public void With_expression_should_allow_changing_label()
    {
        var original = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), 0);
        var labeled = original with { Label = "causal-link" };

        labeled.Label.Should().Be("causal-link");
        original.Label.Should().BeNull("original should be unmodified");
    }

    // -- Dimension -----------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(99)]
    public void Should_store_dimension_index_correctly(int dim)
    {
        var edge = new GridEdge(new GridCoordinate(0, 0), new GridCoordinate(1, 0), dim);
        edge.Dimension.Should().Be(dim);
    }

    // -- High-Dimensional Edges ----------------------------------------------

    [Fact]
    public void Should_connect_high_dimensional_coordinates()
    {
        var source = new GridCoordinate(1, 2, 3, 4, 5);
        var target = new GridCoordinate(1, 2, 3, 4, 6);

        var edge = new GridEdge(source, target, Dimension: 4, Label: "5D-link")
        {
            Weight = 0.42
        };

        edge.Source.Rank.Should().Be(5);
        edge.Target.Rank.Should().Be(5);
        edge.Weight.Should().Be(0.42);
    }

    // -- Self-Loop -----------------------------------------------------------

    [Fact]
    public void Should_allow_self_loop_edge()
    {
        var pos = new GridCoordinate(0, 0);
        var edge = new GridEdge(pos, pos, 0, "self-loop");

        edge.Source.Should().Be(edge.Target);
    }

    // -- ToString (Record Default) -------------------------------------------

    [Fact]
    public void ToString_should_contain_source_and_target_info()
    {
        var edge = new GridEdge(new GridCoordinate(1, 2), new GridCoordinate(3, 4), 0);
        var str = edge.ToString();

        str.Should().NotBeNullOrWhiteSpace();
        // Record ToString should include the type name and property values
        str.Should().Contain("GridEdge");
    }
}
