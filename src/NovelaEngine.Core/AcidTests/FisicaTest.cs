using System;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.Llm;

namespace NovelaEngine.Core.AcidTests;

public class FisicaTest : IAcidTest
{
    public string Nombre => "Física";

    public async Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
    {
        // TODO: Cruza acciones contra Personaje.Restricciones usando LLM
        return await Task.FromResult(new AcidResult(true, null, null));
    }
}
