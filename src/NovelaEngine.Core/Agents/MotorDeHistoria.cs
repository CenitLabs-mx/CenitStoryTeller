using System;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Core.Llm;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Core.Agents;

public class MotorDeHistoria
{
    private readonly ILlmClient _llm;

    public MotorDeHistoria(ILlmClient llm)
    {
        _llm = llm;
    }

    public async Task<CapituloVersion> ProponerEscenaAsync(Obra obra, Beat beat, Capitulo capitulo, CancellationToken ct)
    {
        // TODO: Implement story generation logic using ILlmClient
        return await Task.FromResult(new CapituloVersion
        {
            Id = Guid.NewGuid(),
            CapituloId = capitulo.Id,
            NumeroVersion = 1,
            EsFinal = false,
            CreadoEn = DateTimeOffset.UtcNow,
            Modelo = "DraftModel",
            PromptUsado = "MotorDeHistoria prompt",
            Texto = "Prosa de escena generada..."
        });
    }
}
