using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.AcidTests;
using NovelaEngine.Core.Llm;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Core.Agents;

public class ContinuidadAcido
{
    private readonly ILlmClient _llm;
    private readonly List<IAcidTest> _tests;

    public ContinuidadAcido(ILlmClient llm)
    {
        _llm = llm;
        _tests = new List<IAcidTest>
        {
            new FisicaTest(),
            new PsicologicaTest(),
            new AmbientalTest(),
            new QuimicaTest()
        };
    }

    public async Task<PruebaAcido> ValidarEscenaAsync(AcidContext ctx, Guid capituloVersionId, CancellationToken ct)
    {
        bool fisicaOk = true;
        bool psicologicaOk = true;
        bool ambientalOk = true;
        bool quimicaOk = true;
        var hallazgos = new List<string>();
        var parches = new List<string>();

        foreach (var test in _tests)
        {
            var res = await test.EvaluarAsync(ctx, _llm, ct);
            if (!res.Pasa)
            {
                if (test is FisicaTest) fisicaOk = false;
                if (test is PsicologicaTest) psicologicaOk = false;
                if (test is AmbientalTest) ambientalOk = false;
                if (test is QuimicaTest) quimicaOk = false;

                if (!string.IsNullOrEmpty(res.Hallazgo)) hallazgos.Add($"[{test.Nombre}] {res.Hallazgo}");
                if (!string.IsNullOrEmpty(res.Parche)) parches.Add($"[{test.Nombre}] {res.Parche}");
            }
        }

        bool todoPasa = fisicaOk && psicologicaOk && ambientalOk && quimicaOk;
        Veredicto veredicto = todoPasa ? Veredicto.Aprobada : (hallazgos.Count > 0 && parches.Count > 0 ? Veredicto.AprobadaConParches : Veredicto.Rechazada);

        return new PruebaAcido
        {
            Id = Guid.NewGuid(),
            CapituloVersionId = capituloVersionId,
            Fisica = fisicaOk,
            Psicologica = psicologicaOk,
            Ambiental = ambientalOk,
            Quimica = quimicaOk,
            Veredicto = veredicto,
            Hallazgos = string.Join("\n", hallazgos),
            Parches = string.Join("\n", parches)
        };
    }
}
