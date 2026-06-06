using System.Linq;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Core.AcidTests;

public sealed class PsicologicaTest : AcidTestBase
{
    public override AcidDimension Dimension => AcidDimension.Psicologica;
    protected override string EtiquetaPrompt => "Psicológica";

    protected override string Criterio =>
        "Verifica que las reacciones y decisiones de cada personaje sean coherentes con su " +
        "herida central, su deseo y su necesidad. Detecta comportamiento OOC (fuera de personaje): " +
        "giros emocionales sin motivación sembrada o contrarios a su arco.";

    protected override string ContextoRelevante(AcidContext ctx) =>
        ctx.Presentes.Count == 0
            ? "(sin personajes presentes)"
            : string.Join("\n", ctx.Presentes.Select(p =>
                $"- {p.Nombre} ({Coalesce(p.Arquetipo)}). Herida: {Coalesce(p.HeridaCentral)}. " +
                $"Deseo: {Coalesce(p.Deseo)}. Necesidad: {Coalesce(p.Necesidad)}."));
}
