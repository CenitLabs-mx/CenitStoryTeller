using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CenitStoryTeller.Core.Llm;

public enum LlmRole { System, User, Assistant }

public sealed record LlmMessage(LlmRole Role, string Content)
{
    public static LlmMessage System(string c) => new(LlmRole.System, c);
    public static LlmMessage User(string c) => new(LlmRole.User, c);
    public static LlmMessage Assistant(string c) => new(LlmRole.Assistant, c);
}

public sealed record LlmRequest
{
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    public string? Model { get; init; }          // override puntual; si es null usa el del proveedor
    public double Temperature { get; init; } = 0.8;
    public int? MaxTokens { get; init; }
}

public sealed record LlmResponse(
    string Text,
    string Model,
    int? PromptTokens = null,
    int? CompletionTokens = null);

/// <summary>Cliente unificado de proveedor LLM. Implementado por cada adaptador.</summary>
public interface ILlmClient
{
    string Provider { get; }
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamAsync(LlmRequest request, CancellationToken ct = default);
}
