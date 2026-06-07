using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Llm;

namespace CenitStoryTeller.Core.AcidTests;

public sealed record DimensionResultado(AcidDimension Dimension, AcidResult Resultado);

public interface IAcidTestRunner
{
    Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(AcidContext ctx, CancellationToken ct = default);
}

public sealed class AcidTestRunner : IAcidTestRunner
{
    private readonly IEnumerable<IAcidTest> _tests;
    private readonly ILlmClientFactory _llmFactory;

    public AcidTestRunner(IEnumerable<IAcidTest> tests, ILlmClientFactory llmFactory)
    {
        _tests = tests;
        _llmFactory = llmFactory;
    }

    // Secuencial a propósito: evita rate limits y mantiene el orden de las dimensiones.
    public async Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(
        AcidContext ctx, CancellationToken ct = default)
    {
        var llm = await _llmFactory.ObtenerAsync(ct);
        var resultados = new List<DimensionResultado>();
        foreach (var test in _tests)
        {
            var r = await test.EvaluarAsync(ctx, llm, ct);
            resultados.Add(new DimensionResultado(test.Dimension, r));
        }
        return resultados;
    }
}
