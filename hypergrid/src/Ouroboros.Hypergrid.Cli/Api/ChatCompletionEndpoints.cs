namespace Ouroboros.Hypergrid.Cli.Api;

using System.Text.Json;
using Ouroboros.Hypergrid.Cli.Models;
using Ouroboros.Hypergrid.Cli.Providers;

/// <summary>
/// Maps the OpenAI-compatible /v1/chat/completions endpoint.
/// Supports both batch and SSE streaming responses.
/// </summary>
internal static class ChatCompletionEndpoints
{
    public static void Map(IEndpointRouteBuilder app, PipelineProviderRegistry registry)
    {
        app.MapPost("/v1/chat/completions", async (ChatCompletionRequest request, HttpContext ctx, CancellationToken ct) =>
        {
            var resolved = registry.Resolve(request.Model);
            if (resolved is null)
                return Results.Json(
                    new { error = new { message = $"Model '{request.Model}' not found.", type = "invalid_request_error" } },
                    statusCode: 404);

            var (pipeline, effectiveModel) = resolved.Value;

            // Only override model on passthrough pipelines when they're the direct target.
            // When "iaret" is selected, convergence calls Ollama internally with its default model.
            OllamaHttpPipeline.SetRequestModel(pipeline is OllamaHttpPipeline ? effectiveModel : null);
            UpstreamHttpPipeline.SetRequestModel(pipeline is UpstreamHttpPipeline ? effectiveModel : null);

            var (systemPrompt, userPrompt) = ExtractPrompts(request.Messages);
            var completionId = $"chatcmpl-{Guid.NewGuid():N}";
            var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (request.Stream)
            {
                ctx.Response.ContentType = "text/event-stream";
                ctx.Response.Headers.CacheControl = "no-cache";
                ctx.Response.Headers.Connection = "keep-alive";

                // Role announcement chunk
                await WriteChunkAsync(ctx.Response, new ChatCompletionChunk
                {
                    Id = completionId, Created = created, Model = request.Model,
                    Choices = [new ChunkChoice { Delta = new ChunkDelta { Role = "assistant" } }]
                }, ct);

                // Stream content tokens
                if (pipeline.SupportsStreaming)
                {
                    await foreach (var token in pipeline.StreamAsync(userPrompt, systemPrompt, ct))
                    {
                        await WriteChunkAsync(ctx.Response, new ChatCompletionChunk
                        {
                            Id = completionId, Created = created, Model = request.Model,
                            Choices = [new ChunkChoice { Delta = new ChunkDelta { Content = token } }]
                        }, ct);
                    }
                }
                else
                {
                    var full = await pipeline.GenerateAsync(userPrompt, systemPrompt, ct);
                    await WriteChunkAsync(ctx.Response, new ChatCompletionChunk
                    {
                        Id = completionId, Created = created, Model = request.Model,
                        Choices = [new ChunkChoice { Delta = new ChunkDelta { Content = full } }]
                    }, ct);
                }

                // Finish chunk
                await WriteChunkAsync(ctx.Response, new ChatCompletionChunk
                {
                    Id = completionId, Created = created, Model = request.Model,
                    Choices = [new ChunkChoice { Delta = new ChunkDelta(), FinishReason = "stop" }]
                }, ct);

                await ctx.Response.WriteAsync("data: [DONE]\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
                return Results.Empty;
            }
            else
            {
                var responseText = await pipeline.GenerateAsync(userPrompt, systemPrompt, ct);
                return Results.Json(new ChatCompletionResponse
                {
                    Id = completionId, Created = created, Model = request.Model,
                    Choices = [new ChatChoice { Message = new ChatMessage { Role = "assistant", Content = responseText } }],
                    Usage = new UsageInfo
                    {
                        PromptTokens = EstimateTokens(userPrompt),
                        CompletionTokens = EstimateTokens(responseText)
                    }
                });
            }
        });
    }

    private static async Task WriteChunkAsync(HttpResponse response, ChatCompletionChunk chunk, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(chunk);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    private static (string? SystemPrompt, string UserPrompt) ExtractPrompts(IReadOnlyList<ChatMessage> messages)
    {
        string? systemPrompt = null;
        var userParts = new List<string>();

        foreach (var msg in messages)
        {
            switch (msg.Role)
            {
                case "system":
                    systemPrompt = msg.Content;
                    break;
                case "user":
                    userParts.Add(msg.Content);
                    break;
                case "assistant":
                    userParts.Add($"[Assistant: {msg.Content}]");
                    break;
            }
        }

        return (systemPrompt, string.Join("\n", userParts));
    }

    private static int EstimateTokens(string text) => (text.Length + 3) / 4;
}
