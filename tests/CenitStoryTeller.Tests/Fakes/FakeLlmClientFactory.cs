using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Llm;

namespace CenitStoryTeller.Tests.Fakes;

// Factory que siempre devuelve el mismo FakeLlmClient — el equivalente bajo el
// nuevo contrato ILlmClientFactory para tests que antes inyectaban el cliente
// directamente.
public sealed class FakeLlmClientFactory : ILlmClientFactory
{
    public FakeLlmClient Cliente { get; }
    public FakeLlmClientFactory(FakeLlmClient cliente) => Cliente = cliente;
    public Task<ILlmClient> ObtenerAsync(CancellationToken ct = default)
        => Task.FromResult<ILlmClient>(Cliente);
}

public sealed class FakeLlmOptionsAccessor : ILlmOptionsAccessor
{
    public LlmOptions Options { get; init; } = new();
    public Task<LlmOptions> ObtenerAsync(CancellationToken ct = default) => Task.FromResult(Options);
}
