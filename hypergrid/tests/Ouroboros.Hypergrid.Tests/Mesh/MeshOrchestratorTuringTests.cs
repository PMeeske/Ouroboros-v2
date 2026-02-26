namespace Ouroboros.Hypergrid.Tests.Mesh;

using FluentAssertions;
using NSubstitute;
using Ouroboros.Hypergrid.Mesh;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for MeshOrchestrator — validates the CLI-driven lifecycle
/// management of interconnected Ouroboros pipeline nodes. The mesh orchestrator
/// is the brain that registers reasoning nodes, interwires them across dimensions,
/// and monitors their collective health. If Ouroboros is the serpent, the mesh is
/// its nervous system.
/// </summary>
public sealed class MeshOrchestratorTuringTests
{
    private static readonly DimensionDescriptor Temporal = new(0, "temporal", "Time");
    private static readonly DimensionDescriptor Semantic = new(1, "semantic", "Concepts");
    private static readonly DimensionDescriptor Causal = new(2, "causal", "Cause-effect");

    private static (HypergridSpace Space, IInterwire Wire, MeshOrchestrator Orchestrator) CreateOrchestrator()
    {
        var space = new HypergridSpace([Temporal, Semantic, Causal]);
        var interwire = Substitute.For<IInterwire>();

        interwire.Connect(Arg.Any<OuroborosNode>(), Arg.Any<OuroborosNode>(), Arg.Any<GridEdge>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new StreamConnection(
                Guid.NewGuid().ToString(),
                callInfo.Arg<OuroborosNode>(),
                callInfo.ArgAt<OuroborosNode>(1),
                callInfo.Arg<GridEdge>()));

        var orchestrator = new MeshOrchestrator(space, interwire);
        return (space, interwire, orchestrator);
    }

    // ── Node Registration ───────────────────────────────────────────────

    [Fact]
    public void Register_should_create_node_at_grid_position()
    {
        var (_, _, orch) = CreateOrchestrator();
        var pos = new GridCoordinate(0, 0, 0);

        var node = orch.Register("Ou-1", pos);

        node.Should().NotBeNull();
        node.Id.Should().Be("Ou-1");
        node.Position.Should().Be(pos);
        orch.Nodes.Should().HaveCount(1);
    }

    [Fact]
    public void Register_should_mark_node_as_healthy()
    {
        var (_, _, orch) = CreateOrchestrator();

        var node = orch.Register("Ou-1", new GridCoordinate(0, 0, 0));

        node.Health.Status.Should().Be(NodeStatus.Healthy);
    }

    [Fact]
    public void Register_should_add_cell_to_hypergrid_space()
    {
        var (space, _, orch) = CreateOrchestrator();
        var pos = new GridCoordinate(1, 2, 3);

        orch.Register("Ou-1", pos);

        space.GetCell(pos).Should().NotBeNull();
        space.GetCell(pos)!.NodeId.Should().Be("Ou-1");
    }

    [Fact]
    public void Register_multiple_nodes_should_track_all()
    {
        var (_, _, orch) = CreateOrchestrator();

        orch.Register("Ou-1", new GridCoordinate(0, 0, 0));
        orch.Register("Ou-2", new GridCoordinate(1, 0, 0));
        orch.Register("Ou-3", new GridCoordinate(0, 1, 0));

        orch.Nodes.Should().HaveCount(3);
    }

    // ── Interwiring ─────────────────────────────────────────────────────

    [Fact]
    public async Task Interwire_should_connect_two_nodes()
    {
        var (_, _, orch) = CreateOrchestrator();
        orch.Register("Ou-1", new GridCoordinate(0, 0, 0));
        orch.Register("Ou-2", new GridCoordinate(1, 0, 0));

        var connection = await orch.Interwire("Ou-1", "Ou-2", dimension: 0);

        connection.Should().NotBeNull();
        connection.Source.Id.Should().Be("Ou-1");
        connection.Target.Id.Should().Be("Ou-2");
        connection.IsActive.Should().BeTrue();
        orch.Connections.Should().HaveCount(1);
    }

    [Fact]
    public async Task Interwire_should_call_protocol_connect()
    {
        var (_, wire, orch) = CreateOrchestrator();
        orch.Register("A", new GridCoordinate(0, 0, 0));
        orch.Register("B", new GridCoordinate(1, 0, 0));

        await orch.Interwire("A", "B", 0);

        await wire.Received(1).Connect(
            Arg.Is<OuroborosNode>(n => n.Id == "A"),
            Arg.Is<OuroborosNode>(n => n.Id == "B"),
            Arg.Any<GridEdge>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Interwire_should_add_edge_to_space()
    {
        var (space, _, orch) = CreateOrchestrator();
        orch.Register("A", new GridCoordinate(0, 0, 0));
        orch.Register("B", new GridCoordinate(1, 0, 0));

        await orch.Interwire("A", "B", 0);

        space.Edges.Should().HaveCount(1);
        space.Edges[0].Dimension.Should().Be(0);
    }

    [Fact]
    public async Task Should_support_multiple_interwirings()
    {
        var (_, _, orch) = CreateOrchestrator();
        orch.Register("A", new GridCoordinate(0, 0, 0));
        orch.Register("B", new GridCoordinate(1, 0, 0));
        orch.Register("C", new GridCoordinate(0, 1, 0));

        await orch.Interwire("A", "B", 0);
        await orch.Interwire("A", "C", 1);
        await orch.Interwire("B", "C", 1);

        orch.Connections.Should().HaveCount(3);
    }

    [Fact]
    public async Task Interwire_unknown_node_should_throw()
    {
        var (_, _, orch) = CreateOrchestrator();
        orch.Register("A", new GridCoordinate(0, 0, 0));

        var act = async () => await orch.Interwire("A", "UNKNOWN", 0);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Health Reporting ────────────────────────────────────────────────

    [Fact]
    public void GetHealthReport_should_include_all_nodes()
    {
        var (_, _, orch) = CreateOrchestrator();
        orch.Register("Ou-1", new GridCoordinate(0, 0, 0));
        orch.Register("Ou-2", new GridCoordinate(1, 0, 0));

        var report = orch.GetHealthReport();

        report.Should().HaveCount(2);
        report.Select(h => h.NodeId).Should().Contain(["Ou-1", "Ou-2"]);
    }

    [Fact]
    public void GetHealthReport_should_reflect_node_status_changes()
    {
        var (_, _, orch) = CreateOrchestrator();
        var node = orch.Register("Ou-1", new GridCoordinate(0, 0, 0));

        node.ReportDegraded("high latency");

        var report = orch.GetHealthReport();
        report.Should().ContainSingle().Which.Status.Should().Be(NodeStatus.Degraded);
    }

    [Fact]
    public void GetHealthReport_empty_mesh_should_return_empty()
    {
        var (_, _, orch) = CreateOrchestrator();
        orch.GetHealthReport().Should().BeEmpty();
    }

    // ── Construction Guards ─────────────────────────────────────────────

    [Fact]
    public void Constructor_null_space_should_throw()
    {
        var wire = Substitute.For<IInterwire>();
        var act = () => new MeshOrchestrator(null!, wire);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_null_interwire_should_throw()
    {
        var space = new HypergridSpace([Temporal]);
        var act = () => new MeshOrchestrator(space, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Full Mesh Lifecycle Scenario ────────────────────────────────────

    [Fact]
    public async Task Should_orchestrate_complete_mesh_lifecycle()
    {
        var (space, _, orch) = CreateOrchestrator();

        // Phase 1: Register a 3-node mesh
        var nodeA = orch.Register("Ou-A", new GridCoordinate(0, 0, 0));
        var nodeB = orch.Register("Ou-B", new GridCoordinate(1, 0, 0));
        var nodeC = orch.Register("Ou-C", new GridCoordinate(0, 1, 0));

        orch.Nodes.Should().HaveCount(3);

        // Phase 2: Wire them up
        await orch.Interwire("Ou-A", "Ou-B", 0); // temporal link
        await orch.Interwire("Ou-A", "Ou-C", 1); // semantic link
        await orch.Interwire("Ou-B", "Ou-C", 1); // cross-link

        orch.Connections.Should().HaveCount(3);
        space.Edges.Should().HaveCount(3);

        // Phase 3: All nodes healthy
        orch.GetHealthReport().Should().AllSatisfy(h =>
            h.Status.Should().Be(NodeStatus.Healthy));

        // Phase 4: Node B encounters issues
        nodeB.ReportDegraded("LLM provider timeout");

        var healthReport = orch.GetHealthReport();
        healthReport.Single(h => h.NodeId == "Ou-B").Status.Should().Be(NodeStatus.Degraded);
        healthReport.Single(h => h.NodeId == "Ou-A").Status.Should().Be(NodeStatus.Healthy);
        healthReport.Single(h => h.NodeId == "Ou-C").Status.Should().Be(NodeStatus.Healthy);

        // Phase 5: Node B recovers
        nodeB.ReportHealthy();
        orch.GetHealthReport().Should().AllSatisfy(h =>
            h.Status.Should().Be(NodeStatus.Healthy));
    }
}
