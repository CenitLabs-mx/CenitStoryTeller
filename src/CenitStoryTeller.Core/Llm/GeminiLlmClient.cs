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

public sealed class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opt;
    public string Provider => "gemini";

    public GeminiLlmClient(IHttpClientFactory factory, LlmOptions opt)
    {
        _opt = opt;
        _http = factory.CreateClient(nameof(GeminiLlmClient));
        _http.BaseAddress = new Uri(opt.BaseUrl ?? "https://generativelanguage.googleapis.com/v1beta/");
    }

    // Gemini separa la instrucción de sistema y usa el rol "model" para el asistente.
    private object BuildBody(LlmRequest req)
    {
        var system = string.Join(
            Environment.NewLine + Environment.NewLine,
            req.Messages.Where(m => m.Role == LlmRole.System).Select(m => m.Content));

        var contents = req.Messages
            .Where(m => m.Role != LlmRole.System)
            .Select(m => new
            {
                role = m.Role == LlmRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            }).ToArray();

        return new
        {
            systemInstruction = string.IsNullOrEmpty(system)
                ? null
                : new { parts = new[] { new { text = system } } },
            contents,
            generationConfig = new { temperature = req.Temperature, maxOutputTokens = req.MaxTokens }
        };
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest req, CancellationToken ct = default)
    {
        var model = req.Model ?? _opt.ModelDraft;
        var url = $"models/{model}:generateContent?key={_opt.ApiKey}";
        using var resp = await _http.PostAsJsonAsync(url, BuildBody(req), ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var text = doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        return new LlmResponse(text, model);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest req, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var model = req.Model ?? _opt.ModelDraft;
        var url = $"models/{model}:streamGenerateContent?alt=sse&key={_opt.ApiKey}";
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(BuildBody(req)) };
        using var resp = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;
            using var doc = JsonDocument.Parse(line["data:".Length..].Trim());
            if (doc.RootElement.TryGetProperty("candidates", out var cand) &&
                cand[0].GetProperty("content").GetProperty("parts")[0].TryGetProperty("text", out var t) &&
                t.GetString() is { Length: > 0 } chunk)
                yield return chunk;
        }
    }
}
