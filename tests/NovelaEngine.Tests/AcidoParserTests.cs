using System;
using NovelaEngine.Data.Entities;
using NovelaEngine.Data.Services;
using Xunit;

namespace NovelaEngine.Tests;

public class AcidoParserTests
{
    private const string JsonCompleto = """
    {
      "fisica": true,
      "psicologica": false,
      "ambiental": true,
      "quimica": true,
      "anacronismo": true,
      "fidelidad_funcional": false,
      "veredicto": "Revisar",
      "hallazgos": "El farol de gas no cuadra con la época.",
      "parches": "Cambiar el farol por una lámpara eléctrica."
    }
    """;

    [Fact]
    public void Parse_MapeaBooleanosYTexto()
    {
        var p = AcidoParser.Parse(JsonCompleto);
        Assert.True(p.Fisica);
        Assert.False(p.Psicologica);
        Assert.True(p.Ambiental);
        Assert.True(p.Quimica);
        Assert.True(p.Anacronismo);
        Assert.False(p.FidelidadFuncional);
        Assert.Equal(Veredicto.Revisar, p.Veredicto);
        Assert.Equal("El farol de gas no cuadra con la época.", p.Hallazgos);
        Assert.Equal("Cambiar el farol por una lámpara eléctrica.", p.Parches);
    }

    [Fact]
    public void Parse_SinAnacronismo_DefaultaAFalse()
    {
        var json = """
        { "fisica": true, "psicologica": true, "ambiental": true, "quimica": true, "veredicto": "Aprobado" }
        """;
        var p = AcidoParser.Parse(json);
        Assert.False(p.Anacronismo); // dto.anacronismo ?? false
    }

    [Fact]
    public void Parse_SinFidelidad_DefaultaATrue()
    {
        var json = """
        { "fisica": true, "psicologica": true, "ambiental": true, "quimica": true, "veredicto": "Aprobado" }
        """;
        var p = AcidoParser.Parse(json);
        Assert.True(p.FidelidadFuncional); // dto.fidelidad_funcional ?? true
    }

    [Theory]
    [InlineData("Aprobado", Veredicto.Aprobado)]
    [InlineData("Revisar", Veredicto.Revisar)]
    [InlineData("Rechazado", Veredicto.Rechazado)]
    public void Parse_MapeaVeredictosConocidos(string texto, Veredicto esperado)
    {
        var json = "{ \"fisica\": true, \"psicologica\": true, \"ambiental\": true, "
                 + "\"quimica\": true, \"veredicto\": \"" + texto + "\" }";
        var p = AcidoParser.Parse(json);
        Assert.Equal(esperado, p.Veredicto);
    }

    [Fact]
    public void Parse_VeredictoDesconocido_CaeEnRevisar()
    {
        var json = "{ \"fisica\": true, \"psicologica\": true, \"ambiental\": true, "
                 + "\"quimica\": true, \"veredicto\": \"Approved with Patches\" }";
        var p = AcidoParser.Parse(json);
        Assert.Equal(Veredicto.Revisar, p.Veredicto);
    }

    [Fact]
    public void Parse_ConTextoAlrededor_ExtraeElJson()
    {
        var raw = "Claro, aquí va el veredicto:\n" + JsonCompleto + "\nEspero que ayude.";
        var p = AcidoParser.Parse(raw);
        Assert.Equal(Veredicto.Revisar, p.Veredicto);
    }

    [Fact]
    public void Parse_SinJson_Lanza()
    {
        Assert.Throws<FormatException>(() => AcidoParser.Parse("no hay json por aquí"));
    }
}
