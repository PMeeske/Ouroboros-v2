namespace Ouroboros.Hypergrid.Tests.Composition;

using FluentAssertions;
using Ouroboros.Hypergrid.Streams;
using Ouroboros.Hypergrid.Topology;
using Xunit;

/// <summary>
/// Turing tests for IGridCell composition — validates that processing cells
/// can be composed into pipelines that transform thought streams. This mirrors
/// the Kleisli arrow composition at the heart of Ouroboros: each cell is an
/// arrow A → Stream&lt;B&gt;, and composition means chaining reasoning stages.
///
/// These tests verify that the Hypergrid's distributed processing model
/// correctly implements the "pipeline as topology" paradigm.
/// </summary>
public sealed class GridCellCompositionTuringTests
{
    private static readonly GridCoordinate Origin = new(0, 0, 0);

    private static Thought<T> MakeThought<T>(T payload) => new()
    {
        Payload = payload,
        Origin = Origin,
        Timestamp = DateTimeOffset.UtcNow
    };

    /// <summary>A cell that doubles integer payloads.</summary>
    private sealed class DoubleCell : IGridCell<int, int>
    {
        public async IAsyncEnumerable<Thought<int>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            await foreach (var thought in input.WithCancellation(ct))
                yield return thought.Map(x => x * 2);
        }
    }

    /// <summary>A cell that converts integers to descriptive strings.</summary>
    private sealed class DescribeCell : IGridCell<int, string>
    {
        public async IAsyncEnumerable<Thought<string>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            await foreach (var thought in input.WithCancellation(ct))
                yield return thought.Map(x => $"value={x}");
        }
    }

    /// <summary>A cell that filters out values below a threshold.</summary>
    private sealed class ThresholdCell : IGridCell<int, int>
    {
        private readonly int _threshold;
        public ThresholdCell(int threshold) => _threshold = threshold;

        public async IAsyncEnumerable<Thought<int>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            await foreach (var thought in input.WithCancellation(ct))
            {
                if (thought.Payload >= _threshold)
                    yield return thought;
            }
        }
    }

    /// <summary>A cell that accumulates a running total across thoughts.</summary>
    private sealed class AccumulatorCell : IGridCell<int, int>
    {
        public async IAsyncEnumerable<Thought<int>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            var sum = 0;
            await foreach (var thought in input.WithCancellation(ct))
            {
                sum += thought.Payload;
                yield return thought.Map(_ => sum);
            }
        }
    }

    // ── Single Cell Processing ──────────────────────────────────────────

    [Fact]
    public async Task Single_cell_should_transform_all_thoughts()
    {
        var cell = new DoubleCell();
        var input = ThoughtStream.From([MakeThought(3), MakeThought(7), MakeThought(11)]);

        var results = new List<int>();
        await foreach (var t in cell.Process(input, Origin, CancellationToken.None))
            results.Add(t.Payload);

        results.Should().Equal(6, 14, 22);
    }

    // ── Two-Cell Pipeline (Kleisli-like Composition) ────────────────────

    [Fact]
    public async Task Two_cell_pipeline_should_compose_transformations()
    {
        // double >=> describe  ≈  x -> describe(double(x))
        var doubleCell = new DoubleCell();
        var describeCell = new DescribeCell();

        var input = ThoughtStream.From([MakeThought(5)]);

        // First stage: double
        var intermediate = doubleCell.Process(input, new GridCoordinate(0, 0, 0), CancellationToken.None);

        // Second stage: describe
        var results = new List<string>();
        await foreach (var t in describeCell.Process(intermediate, new GridCoordinate(1, 0, 0), CancellationToken.None))
            results.Add(t.Payload);

        results.Should().ContainSingle().Which.Should().Be("value=10");
    }

    // ── Three-Cell Pipeline ─────────────────────────────────────────────

    [Fact]
    public async Task Three_cell_pipeline_should_chain_correctly()
    {
        // filter(>=5) >=> double >=> describe
        var filterCell = new ThresholdCell(5);
        var doubleCell = new DoubleCell();
        var describeCell = new DescribeCell();

        var input = ThoughtStream.From([MakeThought(3), MakeThought(5), MakeThought(8)]);

        var afterFilter = filterCell.Process(input, new GridCoordinate(0, 0, 0), CancellationToken.None);
        var afterDouble = doubleCell.Process(afterFilter, new GridCoordinate(1, 0, 0), CancellationToken.None);

        var results = new List<string>();
        await foreach (var t in describeCell.Process(afterDouble, new GridCoordinate(2, 0, 0), CancellationToken.None))
            results.Add(t.Payload);

        // 3 filtered out, 5 -> 10, 8 -> 16
        results.Should().Equal("value=10", "value=16");
    }

    // ── Stateful Cell Processing ────────────────────────────────────────

    [Fact]
    public async Task Stateful_cell_should_maintain_state_across_thoughts()
    {
        var accumulator = new AccumulatorCell();
        var input = ThoughtStream.From([MakeThought(10), MakeThought(20), MakeThought(30)]);

        var results = new List<int>();
        await foreach (var t in accumulator.Process(input, Origin, CancellationToken.None))
            results.Add(t.Payload);

        results.Should().Equal(10, 30, 60);
    }

    // ── Empty Input Stream ──────────────────────────────────────────────

    [Fact]
    public async Task Cell_with_empty_input_should_produce_empty_output()
    {
        var cell = new DoubleCell();
        var input = ThoughtStream.From(Array.Empty<Thought<int>>());

        var results = new List<int>();
        await foreach (var t in cell.Process(input, Origin, CancellationToken.None))
            results.Add(t.Payload);

        results.Should().BeEmpty();
    }

    // ── Position Awareness ──────────────────────────────────────────────

    [Fact]
    public async Task Cell_should_receive_its_grid_position()
    {
        // Verify cells get the position parameter
        GridCoordinate? capturedPosition = null;

        var cell = new PositionCapturingCell(pos => capturedPosition = pos);
        var input = ThoughtStream.From([MakeThought(1)]);
        var expected = new GridCoordinate(4, 5, 6);

        await foreach (var _ in cell.Process(input, expected, CancellationToken.None)) { }

        capturedPosition.Should().Be(expected);
    }

    private sealed class PositionCapturingCell : IGridCell<int, int>
    {
        private readonly Action<GridCoordinate> _capture;
        public PositionCapturingCell(Action<GridCoordinate> capture) => _capture = capture;

        public async IAsyncEnumerable<Thought<int>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            _capture(position);
            await foreach (var thought in input.WithCancellation(ct))
                yield return thought;
        }
    }

    // ── Parallel Processing (Fan-Out / Fan-In) ──────────────────────────

    [Fact]
    public async Task Fan_out_fan_in_should_process_parallel_paths()
    {
        // Simulate broadcast routing: one stream processed by two cells, then confluenced
        var addOneCell = new OffsetCell(1);
        var addTenCell = new OffsetCell(10);

        var input = ThoughtStream.From([MakeThought(100)]);

        // Fan out: same input to two cells
        var path1 = addOneCell.Process(input, new GridCoordinate(0, 0, 0), CancellationToken.None);
        var path2 = addTenCell.Process(
            ThoughtStream.From([MakeThought(100)]),
            new GridCoordinate(0, 1, 0),
            CancellationToken.None);

        // Fan in: confluence
        var confluence = new Confluence<int>();
        confluence.Add(path1).Add(path2);
        var batch = await confluence.CollectFirst();

        batch.Should().HaveCount(2);
        batch.Select(t => t.Payload).Should().Contain([101, 110]);
    }

    private sealed class OffsetCell : IGridCell<int, int>
    {
        private readonly int _offset;
        public OffsetCell(int offset) => _offset = offset;

        public async IAsyncEnumerable<Thought<int>> Process(
            IAsyncEnumerable<Thought<int>> input,
            GridCoordinate position,
            CancellationToken ct)
        {
            await foreach (var thought in input.WithCancellation(ct))
                yield return thought.Map(x => x + _offset);
        }
    }
}
