namespace Ouroboros.Hypergrid.Cli.Providers;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Ouroboros.Hypergrid.Host;

/// <summary>
/// HTTP-based pipeline forwarding prompts to an Ollama instance.
/// Supports per-request model override via <see cref="SetRequestModel"/>
/// and true streaming from Ollama's NDJSON response.
/// </summary>
internal sealed class OllamaHttpPipeline : IOuroborosPipeline
{
    private readonly HttpClient _http;
    private readonly Uri _baseUrl;
    private readonly string _defaultModel;
    private static readonly AsyncLocal<string?> s_requestModel = new();

    public OllamaHttpPipeline(Uri baseUrl, string defaultModel = "llama3", string? apiKey = null)
    {
        _baseUrl = baseUrl;
        _defaultModel = defaultModel;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        if (apiKey is not null)
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public string ModelName => $"Ollama@{_baseUrl.Host} ({_defaultModel})";
    public bool SupportsStreaming => true;

    /// <summary>Set a per-request model override (scoped to the current async flow).</summary>
    public static void SetRequestModel(string? model) => s_requestModel.Value = model;

    private string EffectiveModel => s_requestModel.Value ?? _defaultModel;

    public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
    {
        var body = new
        {
            model = EffectiveModel,
            prompt = systemPrompt is not null ? $"[System: {systemPrompt}]\n{prompt}" : prompt,
            stream = false
        };

        var url = new Uri(_baseUrl, "/api/generate");
        var response = await _http.PostAsJsonAsync(url, body, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Ollama {(int)response.StatusCode} at {url} (model: {body.model}): {detail}");
        }
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);
        return result?.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = new
        {
            model = EffectiveModel,
            prompt = systemPrompt is not null ? $"[System: {systemPrompt}]\n{prompt}" : prompt,
            stream = true
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUrl, "/api/generate"))
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Ollama {(int)response.StatusCode} at {request.RequestUri} (model: {body.model}): {detail}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            if (chunk is not null && !string.IsNullOrEmpty(chunk.Response))
                yield return chunk.Response;
            if (chunk?.Done == true)
                yield break;
        }
    }

    private sealed record OllamaResponse(string Response);
    private sealed record OllamaStreamChunk(string? Response, bool Done);
}
