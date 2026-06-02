using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NovelaEngine.Data.Entities;
using NovelaEngine.Core.Llm;

namespace NovelaEngine.Core.AcidTests;

public record AcidContext(
    Capitulo Capitulo,
    string ProsaPropuesta,
    IReadOnlyList<Personaje> Presentes,
    Ubicacion Ubicacion,
    IReadOnlyList<Evento> EventosPrevios);

public record AcidResult(bool Pasa, string? Hallazgo, string? Parche);

public interface IAcidTest
{
    string Nombre { get; }
    Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct);
}
