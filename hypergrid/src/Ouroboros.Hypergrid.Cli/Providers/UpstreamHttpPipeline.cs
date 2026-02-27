namespace Ouroboros.Hypergrid.Cli.Providers;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Ouroboros.Hypergrid.Host;

/// <summary>
/// Generic OpenAI-compatible upstream forwarder.
/// POSTs to {baseUrl}/v1/chat/completions using the standard format.
/// Used for LiteLLM, OpenRouter, vLLM, or any OpenAI-compatible backend.
/// </summary>
internal sealed class UpstreamHttpPipeline : IOuroborosPipeline
{
    private readonly HttpClient _http;
    private readonly string _name;
    private readonly string _defaultModel;
    private static readonly AsyncLocal<string?> s_requestModel = new();

    public UpstreamHttpPipeline(string name, Uri baseUrl, string? apiKey = null, string? defaultModel = null)
    {
        _name = name;
        _defaultModel = defaultModel ?? name;
        _http = new HttpClient { BaseAddress = baseUrl };
        if (apiKey is not null)
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public string ModelName => $"{_name} ({_defaultModel})";
    public bool SupportsStreaming => true;

    /// <summary>Set a per-request model override (scoped to the current async flow).</summary>
    public static void SetRequestModel(string? model) => s_requestModel.Value = model;

    private string EffectiveModel => s_requestModel.Value ?? _defaultModel;

    public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, CancellationToken ct = default)
    {
        var messages = BuildMessages(prompt, systemPrompt);
        var body = new { model = EffectiveModel, messages, stream = false };
        var response = await _http.PostAsJsonAsync("/v1/chat/completions", body, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return ExtractContent(doc);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(prompt, systemPrompt);
        var body = new { model = EffectiveModel, messages, stream = true };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
            var json = line[6..].Trim();
            if (json == "[DONE]") yield break;

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta")
                .TryGetProperty("content", out var c) ? c.GetString() : null;
            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }

    private static object[] BuildMessages(string prompt, string? systemPrompt)
    {
        if (systemPrompt is not null)
            return [new { role = "system", content = systemPrompt }, new { role = "user", content = prompt }];
        return [new { role = "user", content = prompt }];
    }

    private static string ExtractContent(JsonDocument doc)
        => doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
}
