namespace Ouroboros.Hypergrid.Tests.Streams;

using FluentAssertions;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for Thought&lt;T&gt; — validates that a discrete unit of reasoning
/// preserves its identity, metadata, and immutability while supporting functor
/// operations (Map). A thought that flows through a hyperdimensional grid must
/// behave as a well-defined mathematical object.
/// </summary>
public sealed class ThoughtTuringTests
{
    private static readonly GridCoordinate Origin = new(0, 0, 0);

    private static Thought<string> CreateThought(string payload, string? traceId = "trace-1") => new()
    {
        Payload = payload,
        Origin = Origin,
        Timestamp = DateTimeOffset.UtcNow,
        TraceId = traceId,
        Metadata = new Dictionary<string, object> { ["key"] = "value" }
    };

    // ── Construction & Properties ───────────────────────────────────────

    [Fact]
    public void Should_preserve_all_properties()
    {
        var now = DateTimeOffset.UtcNow;
        var thought = new Thought<int>
        {
            Payload = 42,
            Origin = new GridCoordinate(1, 2, 3),
            Timestamp = now,
            TraceId = "abc",
            Metadata = new Dictionary<string, object> { ["dim"] = "temporal" }
        };

        thought.Payload.Should().Be(42);
        thought.Origin.Should().Be(new GridCoordinate(1, 2, 3));
        thought.Timestamp.Should().Be(now);
        thought.TraceId.Should().Be("abc");
        thought.Metadata.Should().ContainKey("dim");
    }

    [Fact]
    public void Nullable_properties_should_default_to_null()
    {
        var thought = new Thought<string>
        {
            Payload = "hello",
            Origin = Origin,
            Timestamp = DateTimeOffset.UtcNow
        };

        thought.TraceId.Should().BeNull();
        thought.Metadata.Should().BeNull();
    }

    // ── Functor Law: Identity ───────────────────────────────────────────

    [Fact]
    public void Map_identity_should_preserve_thought()
    {
        // Functor law: map(id) == id
        var original = CreateThought("hello");
        var mapped = original.Map(x => x);

        mapped.Payload.Should().Be(original.Payload);
        mapped.Origin.Should().Be(original.Origin);
        mapped.Timestamp.Should().Be(original.Timestamp);
        mapped.TraceId.Should().Be(original.TraceId);
        mapped.Metadata.Should().BeEquivalentTo(original.Metadata);
    }

    // ── Functor Law: Composition ────────────────────────────────────────

    [Fact]
    public void Map_composition_should_equal_composed_maps()
    {
        // Functor law: map(f . g) == map(f) . map(g)
        var thought = new Thought<int>
        {
            Payload = 5,
            Origin = Origin,
            Timestamp = DateTimeOffset.UtcNow
        };

        Func<int, int> f = x => x * 2;
        Func<int, int> g = x => x + 3;

        var composed = thought.Map(x => f(g(x)));
        var sequential = thought.Map(g).Map(f);

        composed.Payload.Should().Be(sequential.Payload);
    }

    // ── Map Preserves Metadata ──────────────────────────────────────────

    [Fact]
    public void Map_should_preserve_origin_and_metadata()
    {
        var thought = CreateThought("raw input");
        var transformed = thought.Map(s => s.ToUpperInvariant());

        transformed.Payload.Should().Be("RAW INPUT");
        transformed.Origin.Should().Be(thought.Origin);
        transformed.Timestamp.Should().Be(thought.Timestamp);
        transformed.TraceId.Should().Be(thought.TraceId);
        transformed.Metadata.Should().BeEquivalentTo(thought.Metadata);
    }

    // ── Type Transformation ─────────────────────────────────────────────

    [Fact]
    public void Map_should_support_cross_type_transformation()
    {
        var thought = new Thought<string>
        {
            Payload = "42",
            Origin = Origin,
            Timestamp = DateTimeOffset.UtcNow
        };

        var intThought = thought.Map(int.Parse);
        intThought.Payload.Should().Be(42);
    }

    [Fact]
    public void Map_should_support_complex_type_transformations()
    {
        var thought = new Thought<string>
        {
            Payload = "hello world",
            Origin = Origin,
            Timestamp = DateTimeOffset.UtcNow
        };

        var analyzed = thought.Map(s => new { Words = s.Split(' ').Length, Chars = s.Length });
        analyzed.Payload.Words.Should().Be(2);
        analyzed.Payload.Chars.Should().Be(11);
    }

    // ── Structural Equality ─────────────────────────────────────────────

    [Fact]
    public void Thoughts_with_same_values_should_be_structurally_equal()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var a = new Thought<string>
        {
            Payload = "test",
            Origin = new GridCoordinate(1, 2),
            Timestamp = timestamp,
            TraceId = "t1"
        };
        var b = new Thought<string>
        {
            Payload = "test",
            Origin = new GridCoordinate(1, 2),
            Timestamp = timestamp,
            TraceId = "t1"
        };

        a.Should().Be(b);
    }

    [Fact]
    public void Thoughts_with_different_payloads_should_not_be_equal()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var a = new Thought<string>
        {
            Payload = "alpha",
            Origin = Origin,
            Timestamp = timestamp
        };
        var b = new Thought<string>
        {
            Payload = "beta",
            Origin = Origin,
            Timestamp = timestamp
        };

        a.Should().NotBe(b);
    }

    // ── Immutability ────────────────────────────────────────────────────

    [Fact]
    public void Map_should_return_new_instance_not_mutate_original()
    {
        var original = CreateThought("original");
        var mapped = original.Map(s => "transformed");

        original.Payload.Should().Be("original");
        mapped.Payload.Should().Be("transformed");
    }
}
