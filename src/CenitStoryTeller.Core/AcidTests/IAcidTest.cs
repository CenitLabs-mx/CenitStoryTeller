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
    IReadOnlyList<Evento> EventosPrevios)
{
    // Resumen condensado de capítulos previos aprobados — para que los validadores
    // detecten contradicciones hacia atrás. Vacío si aún no hay capítulos aprobados.
    public string Pasado { get; init; } = "";

    // Beats pendientes después del capítulo bajo análisis — para detectar si la
    // escena cierra prematuramente una trama futura o rompe un setup.
    public string Futuro { get; init; } = "";
}

public record AcidResult(bool Pasa, string? Hallazgo, string? Parche);

public interface IAcidTest
{
    AcidDimension Dimension { get; }
    Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct);
}
