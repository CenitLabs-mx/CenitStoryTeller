using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Llm;

namespace CenitStoryTeller.Core.AcidTests;

public sealed record DimensionResultado(string Dimension, AcidResult Resultado);

public interface IAcidTestRunner
{
    Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(AcidContext ctx, CancellationToken ct = default);
}

public sealed class AcidTestRunner : IAcidTestRunner
{
    private readonly IEnumerable<IAcidTest> _tests;
    private readonly ILlmClient _llm;

    public AcidTestRunner(IEnumerable<IAcidTest> tests, ILlmClient llm)
    {
        _tests = tests;
        _llm = llm;
    }

    // Secuencial a propósito: evita rate limits y mantiene el orden de las dimensiones.
    public async Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(
        AcidContext ctx, CancellationToken ct = default)
    {
        var resultados = new List<DimensionResultado>();
        foreach (var test in _tests)
        {
            var r = await test.EvaluarAsync(ctx, _llm, ct);
            resultados.Add(new DimensionResultado(test.Nombre, r));
        }
        return resultados;
    }
}
