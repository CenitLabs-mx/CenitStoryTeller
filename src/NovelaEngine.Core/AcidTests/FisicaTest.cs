using System.Linq;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Core.AcidTests;

public sealed class FisicaTest : AcidTestBase
{
    public override string Nombre => "Física";

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
