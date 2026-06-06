using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;

namespace CenitStoryTeller.Core.AcidTests;

public enum AcidDimension
{
    Fisica,
    Psicologica,
    Ambiental,
    Quimica
}

public record AcidContext(
    Capitulo Capitulo,
    string ProsaPropuesta,
    IReadOnlyList<Personaje> Presentes,
    Ubicacion Ubicacion,
    IReadOnlyList<Evento> EventosPrevios);

public record AcidResult(bool Pasa, string? Hallazgo, string? Parche);

public interface IAcidTest
{
    AcidDimension Dimension { get; }
    Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct);
}
