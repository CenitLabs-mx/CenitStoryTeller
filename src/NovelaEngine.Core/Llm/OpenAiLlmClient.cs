using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NovelaEngine.Core.Llm;

public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opt;
    public string Provider => "openai";

    public OpenAiLlmClient(IHttpClientFactory factory, LlmOptions opt)
    {
        _opt = opt;
        _http = factory.CreateClient(nameof(OpenAiLlmClient));
        _http.BaseAddress = new Uri(opt.BaseUrl ?? "https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opt.ApiKey);
    }

    private static string Role(LlmRole r) => r switch
    {
        LlmRole.System => "system",
        LlmRole.Assistant => "assistant",
        _ => "user"
    };

    private object BuildBody(LlmRequest req, bool stream) => new
    {
        model = req.Model ?? _opt.ModelDraft,
        temperature = req.Temperature,
        max_tokens = req.MaxTokens,
        stream,
        messages = req.Messages.Select(m => new { role = Role(m.Role), content = m.Content }).ToArray()
    };

    public async Task<LlmResponse> CompleteAsync(LlmRequest req, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("chat/completions", BuildBody(req, false), ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var model = root.GetProperty("model").GetString() ?? req.Model ?? _opt.ModelDraft;
        int? pt = null, ctok = null;
        if (root.TryGetProperty("usage", out var u))
        {
            pt = u.GetProperty("prompt_tokens").GetInt32();
            ctok = u.GetProperty("completion_tokens").GetInt32();
        }
        return new LlmResponse(text, model, pt, ctok);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest req, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(BuildBody(req, true))
        };
        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;
            var payload = line["data:".Length..].Trim();
            if (payload == "[DONE]") yield break;
            using var doc = JsonDocument.Parse(payload);
            var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var c) && c.GetString() is { Length: > 0 } chunk)
                yield return chunk;
        }
    }
}
