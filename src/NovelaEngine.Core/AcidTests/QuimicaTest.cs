using System;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.Llm;

namespace NovelaEngine.Core.AcidTests;

public class QuimicaTest : IAcidTest
{
    public string Nombre => "Química";

    public async Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
    {
        // TODO: Relaciones ganadas vs forzadas/cheesy usando LLM
        return await Task.FromResult(new AcidResult(true, null, null));
    }
}
