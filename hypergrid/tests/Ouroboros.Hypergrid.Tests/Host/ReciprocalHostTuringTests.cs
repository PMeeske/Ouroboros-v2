namespace Ouroboros.Hypergrid.Tests.Host;

using System.Runtime.CompilerServices;
using FluentAssertions;
using Ouroboros.Hypergrid.Host;
using Ouroboros.Hypergrid.Iaret;
using Ouroboros.Hypergrid.Simulation;
using Xunit;

/// <summary>
/// Turing tests for the reciprocal CLI host bridge.
///
/// Validates that both directions work:
///   1. Engine → Iaret: a pipeline is usable as an IIaretEnvironment
///   2. Iaret → Engine: the convergent identity is usable as an IOuroborosPipeline
///   3. Full round-trip: pipeline → Iaret → pipeline adapter → consumer
/// </summary>
public sealed class ReciprocalHostTuringTests : IDisposable
{
    private readonly FakePipeline _pipeline;
    private readonly IaretCliHost _host;

    public ReciprocalHostTuringTests()
    {
        _pipeline = new FakePipeline("FakeModel/test-v1");
        _host = IaretCliHost.Create(_pipeline, SimulatorFactory.CreateCpu());
    }

    // ── Engine → Iaret direction ─────────────────────────────────────────

    [Fact]
    public void OuroborosEnvironment_should_implement_IIaretEnvironment()
    {
        var env = new OuroborosEnvironment(_pipeline);
        env.Should().BeAssignableTo<IIaretEnvironment>();
        env.Name.Should().Be("FakeModel/test-v1");
    }

    [Fact]
    public async Task OuroborosEnvironment_should_delegate_to_pipeline()
    {
        var env = new OuroborosEnvironment(_pipeline);
        var context = new AspectContext
        {
            AspectId = "test",
            SystemPrompt = "You are a test."
        };

        var result = await env.ProcessAsync("hello", context);

        result.Should().Contain("hello");
        result.Should().Contain("[FakeModel/test-v1]");
        _pipeline.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task OuroborosEnvironment_should_include_history_when_present()
    {
        var env = new OuroborosEnvironment(_pipeline);
        var context = new AspectContext
        {
            AspectId = "temporal",
            SystemPrompt = "You are temporal.",
            History = ["first thought", "second thought"]
        };

        var result = await env.ProcessAsync("third thought", context);

        result.Should().Contain("first thought");
        result.Should().Contain("second thought");
        result.Should().Contain("third thought");
    }

    [Fact]
    public async Task OuroborosEnvironment_should_use_tool_aware_path_when_available()
    {
        var toolPipeline = new FakeToolPipeline("ToolModel/v2");
        var env = new OuroborosEnvironment(toolPipeline);
        var context = new AspectContext
        {
            AspectId = "analytical",
            SystemPrompt = "Analyze this."
        };

        var result = await env.ProcessAsync("test input", context);

        result.Should().Contain("[TOOLS]");
        toolPipeline.ToolCallCount.Should().Be(1);
    }

    [Fact]
    public async Task OuroborosEnvironment_should_stream_when_supported()
    {
        var env = new OuroborosEnvironment(_pipeline);
        var context = new AspectContext
        {
            AspectId = "creative",
            SystemPrompt = "Be creative."
        };

        var chunks = new List<string>();
        await foreach (var chunk in env.StreamAsync("stream this", context))
            chunks.Add(chunk);

        chunks.Should().HaveCountGreaterThan(0);
    }

    // ── Iaret → Engine direction (reciprocal) ────────────────────────────

    [Fact]
    public void IaretPipelineAdapter_should_implement_IOuroborosPipeline()
    {
        _host.Pipeline.Should().BeAssignableTo<IOuroborosPipeline>();
        _host.Pipeline.ModelName.Should().StartWith("Iaret/");
    }

    [Fact]
    public async Task IaretPipelineAdapter_should_run_full_convergence()
    {
        var result = await _host.Pipeline.GenerateAsync("What is emergence?");

        result.Should().NotBeNullOrEmpty();
        // The fake pipeline returns tagged responses that flow through aspects,
        // and the synthesis merges them — we should see convergent output
        result.Should().Contain("SYNTHESIS");
    }

    [Fact]
    public async Task IaretPipelineAdapter_should_support_system_prompt()
    {
        var result = await _host.Pipeline.GenerateAsync(
            "test input",
            systemPrompt: "You are a philosopher.");

        result.Should().NotBeNullOrEmpty();
        // The system prompt is prepended to input before convergence
    }

    [Fact]
    public async Task IaretPipelineAdapter_should_expose_aspect_queries()
    {
        var analytical = await _host.Pipeline.QueryAspectAsync("analytical", "The quick brown fox");

        analytical.Should().Contain("ANALYTICAL");
    }

    [Fact]
    public async Task IaretPipelineAdapter_should_support_streaming()
    {
        _host.Pipeline.SupportsStreaming.Should().BeTrue();

        var chunks = new List<string>();
        await foreach (var chunk in _host.Pipeline.StreamAsync("stream test"))
            chunks.Add(chunk);

        chunks.Should().HaveCount(1); // Convergence yields single batch result
        chunks[0].Should().Contain("SYNTHESIS");
    }

    [Fact]
    public async Task IaretPipelineAdapter_should_stream_multiple_prompts()
    {
        var prompts = ToAsyncEnumerable("first thought", "second thought", "third thought");
        var results = new List<string>();

        await foreach (var result in _host.Pipeline.GenerateStreamAsync(prompts))
            results.Add(result);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Should().Contain("SYNTHESIS"));
    }

    // ── Full round-trip ──────────────────────────────────────────────────

    [Fact]
    public async Task Full_round_trip_pipeline_through_iaret_and_back()
    {
        // The pipeline powers the Iaret aspects (Engine → Iaret)
        _host.Environment.Name.Should().Be("FakeModel/test-v1");
        _host.Convergence.Aspects.Should().HaveCount(4);

        // Think through the convergent identity
        var convergentResult = await _host.ThinkAsync("Explain consciousness");
        convergentResult.Should().Contain("SYNTHESIS");

        // The adapter exposes convergent output as IOuroborosPipeline (Iaret → Engine)
        var pipelineResult = await _host.Pipeline.GenerateAsync("Same question via pipeline");
        pipelineResult.Should().Contain("SYNTHESIS");

        // Both directions used the same underlying pipeline
        _pipeline.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_support_aspect_interaction_through_host()
    {
        var analytical = await _host.AskAspectAsync("analytical", "test");
        analytical.Should().Contain("ANALYTICAL");

        var creative = await _host.AskAspectAsync("creative", "test");
        creative.Should().Contain("CREATIVE");

        var guardian = await _host.AskAspectAsync("guardian", "The architecture is sound and well structured");
        guardian.Should().Contain("GUARDIAN");
    }

    // ── IaretCliHost factory methods ─────────────────────────────────────

    [Fact]
    public void CreateLocal_should_use_local_environment()
    {
        using var local = IaretCliHost.CreateLocal(SimulatorFactory.CreateCpu());
        local.Environment.Should().BeOfType<LocalIaretEnvironment>();
        local.Environment.Name.Should().Be("Local");
    }

    [Fact]
    public void Create_with_environment_should_accept_custom_environment()
    {
        var env = new LocalIaretEnvironment();
        using var host = IaretCliHost.Create(env, SimulatorFactory.CreateCpu());
        host.Environment.Should().BeSameAs(env);
    }

    [Fact]
    public void Create_with_pipeline_should_wrap_as_environment()
    {
        _host.Environment.Should().BeOfType<OuroborosEnvironment>();
        _host.Environment.Name.Should().Be("FakeModel/test-v1");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(
        params string[] items)
    {
        foreach (var item in items)
        {
            await Task.CompletedTask;
            yield return item;
        }
    }

    public void Dispose() => _host.Dispose();

    // ── Fakes ────────────────────────────────────────────────────────────

    /// <summary>Fake pipeline that echoes input with a tag.</summary>
    private sealed class FakePipeline(string modelName) : IOuroborosPipeline
    {
        public string ModelName => modelName;
        public bool SupportsStreaming => true;
        public int CallCount { get; private set; }

        public Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            CallCount++;
            var sys = systemPrompt is not null ? $" sys=\"{systemPrompt}\"" : "";
            return Task.FromResult($"[{modelName}{sys}] {prompt}");
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string prompt,
            string? systemPrompt = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await GenerateAsync(prompt, systemPrompt, ct);
            // Yield word by word to simulate streaming
            foreach (var word in result.Split(' '))
            {
                yield return word + " ";
            }
        }
    }

    /// <summary>Fake tool-aware pipeline.</summary>
    private sealed class FakeToolPipeline(string modelName) : IToolAwarePipeline
    {
        public string ModelName => modelName;
        public bool SupportsStreaming => false;
        public int ToolCallCount { get; private set; }

        public Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult($"[{modelName}] {prompt}");

        public Task<string> GenerateWithToolsAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            ToolCallCount++;
            return Task.FromResult($"[{modelName}][TOOLS] {prompt}");
        }

        public IAsyncEnumerable<string> StreamAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
