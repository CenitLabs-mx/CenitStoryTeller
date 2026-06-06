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

namespace CenitStoryTeller.Core.Llm;

public sealed class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opt;
    public string Provider => "ollama";

    public OllamaLlmClient(IHttpClientFactory factory, LlmOptions opt)
    {
        _opt = opt;
        _http = factory.CreateClient(nameof(OllamaLlmClient));
        _http.BaseAddress = new Uri(opt.BaseUrl ?? "http://localhost:11434/");
        _http.Timeout = TimeSpan.FromMinutes(10); // modelos locales pueden tardar
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
        stream,
        options = new { temperature = req.Temperature },
        messages = req.Messages.Select(m => new { role = Role(m.Role), content = m.Content }).ToArray()
    };

    public async Task<LlmResponse> CompleteAsync(LlmRequest req, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("api/chat", BuildBody(req, false), ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var text = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        return new LlmResponse(text, req.Model ?? _opt.ModelDraft);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest req, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(BuildBody(req, true))
        };
        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);   // Ollama emite NDJSON (un JSON por línea)
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var c) &&
                c.GetString() is { Length: > 0 } chunk)
                yield return chunk;
            if (root.TryGetProperty("done", out var done) && done.GetBoolean()) yield break;
        }
    }
}
