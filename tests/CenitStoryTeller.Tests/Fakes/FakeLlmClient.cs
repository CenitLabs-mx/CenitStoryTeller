using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Llm;

namespace CenitStoryTeller.Tests.Fakes;

/// LLM cliente programable. Devuelve respuestas en FIFO y guarda cada request
/// para poder aseverar sobre lo que el servicio le pidió.
public sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<LlmResponse> _respuestas = new();
    public string Provider => "fake";
    public List<LlmRequest> Requests { get; } = new();
    public string ModeloPorDefecto { get; init; } = "fake-model";

    public FakeLlmClient EncolarTexto(string text, string? model = null)
    {
        _respuestas.Enqueue(new LlmResponse(text, model ?? ModeloPorDefecto));
        return this;
    }

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Requests.Add(request);
        if (_respuestas.Count == 0)
            throw new InvalidOperationException("FakeLlmClient: no quedan respuestas encoladas.");
        return Task.FromResult(_respuestas.Dequeue());
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var resp = await CompleteAsync(request, ct);
        yield return resp.Text;
    }
}
