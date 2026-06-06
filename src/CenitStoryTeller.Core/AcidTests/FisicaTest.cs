using System.Linq;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Core.AcidTests;

public sealed class FisicaTest : AcidTestBase
{
    public override AcidDimension Dimension => AcidDimension.Fisica;
    protected override string EtiquetaPrompt => "Física";

    protected override string Criterio =>
        "Verifica que el cuerpo de cada personaje tolere lo que hace y que no se violen sus " +
        "restricciones físicas (heridas, límites corporales, edad, capacidades). " +
        "Un personaje con estado vital Muerto no puede actuar ni aparecer vivo en escena.";

    protected override string ContextoRelevante(AcidContext ctx) =>
        ctx.Presentes.Count == 0
            ? "(sin personajes presentes)"
            : string.Join("\n", ctx.Presentes.Select(p =>
                $"- {p.Nombre} ({Coalesce(p.Rol)}). Estado vital: {p.EstadoVital}. " +
                $"Restricciones: {Coalesce(p.Restricciones)}."));
}
