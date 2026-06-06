namespace CenitStoryTeller.Core.AcidTests;

public sealed class AmbientalTest : AcidTestBase
{
    public override string Nombre => "Ambiental";

    protected override string Criterio =>
        "Verifica que lo narrado sea plausible en este lugar y época, y que los testigos y el " +
        "entorno reaccionen según las reglas del lugar (lore): física, leyes, normas sociales. " +
        "Marca cualquier elemento que choque con el estado actual de la ubicación.";

    protected override string ContextoRelevante(AcidContext ctx) =>
        $"Ubicación: {UbicacionBreve(ctx)}\n" +
        $"Plataforma/registro objetivo: {Coalesce(ctx.Capitulo.Plataforma)}";
}
