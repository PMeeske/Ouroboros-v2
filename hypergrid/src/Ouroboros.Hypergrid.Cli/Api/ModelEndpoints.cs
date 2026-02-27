namespace Ouroboros.Hypergrid.Cli.Api;

using Ouroboros.Hypergrid.Cli.Models;
using Ouroboros.Hypergrid.Cli.Providers;

/// <summary>
/// Maps the OpenAI-compatible /v1/models endpoint.
/// </summary>
internal static class ModelEndpoints
{
    public static void Map(IEndpointRouteBuilder app, PipelineProviderRegistry registry)
    {
        app.MapGet("/v1/models", () =>
        {
            var models = registry.ListProviders()
                .Select(p => new ModelInfo
                {
                    Id = p.Prefix,
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                })
                .ToList();

            return Results.Json(new ModelListResponse { Data = models });
        });

        app.MapGet("/v1/models/{modelId}", (string modelId) =>
        {
            var resolved = registry.Resolve(modelId);
            if (resolved is null)
                return Results.NotFound(new { error = new { message = $"Model '{modelId}' not found." } });

            return Results.Json(new ModelInfo
            {
                Id = modelId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        });
    }
}
