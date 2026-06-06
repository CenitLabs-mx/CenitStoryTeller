using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Data.Services;

namespace CenitStoryTeller.Tests.Fakes;

public sealed class FakePromptProvider : IPromptProvider
{
    public string MotorDeHistoria { get; init; } = "SYSTEM: motor";
    public string ContinuidadAcido { get; init; } = "SYSTEM: acido";
    public string Moderniza { get; init; } = "SYSTEM: moderniza";

    public Task<string> MotorDeHistoriaAsync(CancellationToken ct = default) => Task.FromResult(MotorDeHistoria);
    public Task<string> ContinuidadAcidoAsync(CancellationToken ct = default) => Task.FromResult(ContinuidadAcido);
    public Task<string> ModernizaAsync(CancellationToken ct = default) => Task.FromResult(Moderniza);
}
