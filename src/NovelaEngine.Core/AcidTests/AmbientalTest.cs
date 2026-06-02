using System;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.Llm;

namespace NovelaEngine.Core.AcidTests;

public class AmbientalTest : IAcidTest
{
    public string Nombre => "Ambiental";

    public async Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
    {
        // TODO: Plausibilidad del lugar + reacción de "metiches" usando LLM
        return await Task.FromResult(new AcidResult(true, null, null));
    }
}
