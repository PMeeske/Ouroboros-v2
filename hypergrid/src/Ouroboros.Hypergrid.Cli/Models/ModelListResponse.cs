namespace Ouroboros.Hypergrid.Cli.Models;

using System.Text.Json.Serialization;

/// <summary>
/// OpenAI-compatible /v1/models response.
/// </summary>
public sealed record ModelListResponse
{
    [JsonPropertyName("object")]
    public string Object => "list";

    [JsonPropertyName("data")]
    public required IReadOnlyList<ModelInfo> Data { get; init; }
}

public sealed record ModelInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("object")]
    public string Object => "model";

    [JsonPropertyName("created")]
    public long Created { get; init; }

    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; init; } = "ouroboros-hypergrid";
}
