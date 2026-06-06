using System.Linq;

namespace CenitStoryTeller.Core.AcidTests;

public sealed class QuimicaTest : AcidTestBase
{
    public override string Nombre => "Química";

    protected override string Criterio =>
        "Verifica que las relaciones (romance, alianza, rivalidad) se sientan ganadas y construidas. " +
        "Marca como forzada cualquier dinámica sin siembra previa en los eventos, o demasiado cursi/" +
        "acelerada respecto a lo que ya ocurrió en la historia.";

    protected override string ContextoRelevante(AcidContext ctx)
    {
        var nombres = string.Join(", ", ctx.Presentes.Select(p => p.Nombre));
        return $"Personajes presentes: {nombres}\n\n" +
               $"Eventos previos (siembra disponible):\n{EventosBreve(ctx)}";
    }
}
