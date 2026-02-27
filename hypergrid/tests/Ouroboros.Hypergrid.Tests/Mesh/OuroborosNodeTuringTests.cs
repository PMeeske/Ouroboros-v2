namespace Ouroboros.Hypergrid.Tests.Mesh;

using FluentAssertions;
using Ouroboros.Hypergrid.Mesh;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for OuroborosNode — validates that individual nodes in the
/// Hypergrid mesh correctly manage their identity, position, and health lifecycle.
/// Each node wraps an Ouroboros pipeline instance; it must faithfully report its
/// health and transition cleanly through operational states.
/// </summary>
public sealed class OuroborosNodeTuringTests
{
    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Should_preserve_id_and_position()
    {
        var pos = new GridCoordinate(3, 7, 1);
        var node = new OuroborosNode("Ou-42", pos);

        node.Id.Should().Be("Ou-42");
        node.Position.Should().Be(pos);
    }

    [Fact]
    public void New_node_should_have_unknown_health_status()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));
        node.Health.Status.Should().Be(NodeStatus.Unknown);
    }

    [Fact]
    public void Constructor_null_id_should_throw()
    {
        var act = () => new OuroborosNode(null!, new GridCoordinate(0));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_null_position_should_throw()
    {
        var act = () => new OuroborosNode("Ou-1", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Health Status Transitions ───────────────────────────────────────

    [Fact]
    public void ReportHealthy_should_set_healthy_status_and_update_heartbeat()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));
        var before = DateTimeOffset.UtcNow;

        node.ReportHealthy();

        node.Health.Status.Should().Be(NodeStatus.Healthy);
        node.Health.LastHeartbeat.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ReportDegraded_should_set_status_and_reason()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));

        node.ReportDegraded("high memory usage");

        node.Health.Status.Should().Be(NodeStatus.Degraded);
        node.Health.StatusReason.Should().Be("high memory usage");
    }

    [Fact]
    public void ReportFaulted_should_set_status_and_reason()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));

        node.ReportFaulted("connection lost");

        node.Health.Status.Should().Be(NodeStatus.Faulted);
        node.Health.StatusReason.Should().Be("connection lost");
    }

    [Fact]
    public void Should_support_full_health_lifecycle()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));

        // Initially unknown
        node.Health.Status.Should().Be(NodeStatus.Unknown);

        // Comes online
        node.ReportHealthy();
        node.Health.Status.Should().Be(NodeStatus.Healthy);

        // Experiences load
        node.ReportDegraded("LLM queue depth > 100");
        node.Health.Status.Should().Be(NodeStatus.Degraded);

        // Crashes
        node.ReportFaulted("out of memory");
        node.Health.Status.Should().Be(NodeStatus.Faulted);

        // Recovers
        node.ReportHealthy();
        node.Health.Status.Should().Be(NodeStatus.Healthy);
    }

    [Fact]
    public void Health_heartbeat_should_update_on_each_status_report()
    {
        var node = new OuroborosNode("Ou-1", new GridCoordinate(0, 0, 0));
        var initial = node.Health.LastHeartbeat;

        node.ReportHealthy();
        var afterHealthy = node.Health.LastHeartbeat;
        afterHealthy.Should().BeAfter(initial);

        node.ReportDegraded("test");
        var afterDegraded = node.Health.LastHeartbeat;
        afterDegraded.Should().BeOnOrAfter(afterHealthy);
    }

    // ── NodeHealth Record Semantics ─────────────────────────────────────

    [Fact]
    public void NodeHealth_should_preserve_node_id()
    {
        var health = new NodeHealth("Ou-42");
        health.NodeId.Should().Be("Ou-42");
    }

    [Fact]
    public void NodeHealth_defaults_should_be_sensible()
    {
        var health = new NodeHealth("test");

        health.Status.Should().Be(NodeStatus.Unknown);
        health.StatusReason.Should().BeNull();
        health.LastHeartbeat.Should().Be(DateTimeOffset.MinValue);
        health.ProcessedCount.Should().Be(0);
        health.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void NodeHealth_with_operator_should_create_updated_copy()
    {
        var original = new NodeHealth("Ou-1");
        var updated = original with { Status = NodeStatus.Healthy, ProcessedCount = 100 };

        original.Status.Should().Be(NodeStatus.Unknown, "original should be immutable");
        updated.Status.Should().Be(NodeStatus.Healthy);
        updated.ProcessedCount.Should().Be(100);
        updated.NodeId.Should().Be("Ou-1");
    }

    // ── StreamConnection ────────────────────────────────────────────────

    [Fact]
    public void StreamConnection_should_preserve_connection_details()
    {
        var source = new OuroborosNode("A", new GridCoordinate(0, 0, 0));
        var target = new OuroborosNode("B", new GridCoordinate(1, 0, 0));
        var edge = new GridEdge(source.Position, target.Position, 0);

        var conn = new StreamConnection("conn-1", source, target, edge);

        conn.ConnectionId.Should().Be("conn-1");
        conn.Source.Id.Should().Be("A");
        conn.Target.Id.Should().Be("B");
        conn.Edge.Dimension.Should().Be(0);
        conn.IsActive.Should().BeTrue();
    }

    [Fact]
    public void StreamConnection_established_at_should_default_to_now()
    {
        var before = DateTimeOffset.UtcNow;
        var conn = new StreamConnection(
            "conn-1",
            new OuroborosNode("A", new GridCoordinate(0)),
            new OuroborosNode("B", new GridCoordinate(1)),
            new GridEdge(new GridCoordinate(0), new GridCoordinate(1), 0));

        conn.EstablishedAt.Should().BeOnOrAfter(before);
    }
}
