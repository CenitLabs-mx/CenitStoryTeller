using System;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.Llm;

namespace NovelaEngine.Core.AcidTests;

public class PsicologicaTest : IAcidTest
{
    public string Nombre => "Psicológica";

    public async Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
    {
        // TODO: Coherencia con herida/deseo/necesidad (detecta OOC) usando LLM
        return await Task.FromResult(new AcidResult(true, null, null));
    }
}
