using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.AcidTests;

// Cubrimos AcidTestBase.Parse a través de EvaluarAsync de un test concreto: lo importante
// es el contrato (qué AcidResult sale) ante distintas formas de respuesta del LLM.
public class AcidTestBaseParseTests
{
    private static AcidContext CtxMinimo() =>
        new(
            new Capitulo { Titulo = "Cap 1", Orden = 1 },
            "prosa de prueba",
            new List<Personaje> { new() { Nombre = "Ana", EstadoVital = EstadoVital.Vivo } },
            new Ubicacion { Nombre = "Plaza", Tipo = "exterior" },
            new List<Evento>());

    private static async Task<AcidResult> Evaluar(string respuestaLlm)
    {
        var llm = new FakeLlmClient().EncolarTexto(respuestaLlm);
        return await new FisicaTest().EvaluarAsync(CtxMinimo(), llm, CancellationToken.None);
    }

    [Fact]
    public async Task Parse_JsonValidoFallido_DevuelveHallazgoYParche()
    {
        var r = await Evaluar("""
            { "pasa": false, "hallazgo": "Ana está muerta pero camina.", "parche": "Marcar a Ana como viva o ajustar el ENV." }
        """);

        Assert.False(r.Pasa);
        Assert.Equal("Ana está muerta pero camina.", r.Hallazgo);
        Assert.Equal("Marcar a Ana como viva o ajustar el ENV.", r.Parche);
    }

    [Fact]
    public async Task Parse_PasaTrue_NoTraeHallazgo()
    {
        var r = await Evaluar("""{ "pasa": true, "hallazgo": null, "parche": null }""");

        Assert.True(r.Pasa);
        Assert.Null(r.Hallazgo);
        Assert.Null(r.Parche);
    }

    [Fact]
    public async Task Parse_PasaComoString_TolerantePasa()
    {
        // Algunos modelos devuelven "true" en vez de booleano JSON.
        var r = await Evaluar("""{ "pasa": "true", "hallazgo": null, "parche": null }""");
        Assert.True(r.Pasa);
    }

    [Fact]
    public async Task Parse_FaltaCampoPasa_DefaultTrue()
    {
        // No bloqueamos el pipeline si el LLM omite el campo "pasa".
        var r = await Evaluar("""{ "hallazgo": "ojo", "parche": "ojo" }""");
        Assert.True(r.Pasa);
    }

    [Fact]
    public async Task Parse_ConPrefacioYSufijo_ExtraeElPrimerObjeto()
    {
        var raw = "Claro, aquí va el veredicto:\n" +
                  """{ "pasa": false, "hallazgo": "x", "parche": "y" }""" +
                  "\nEspero que ayude.";
        var r = await Evaluar(raw);

        Assert.False(r.Pasa);
        Assert.Equal("x", r.Hallazgo);
    }

    [Fact]
    public async Task Parse_RespuestaVacia_PasaPorDefecto()
    {
        var r = await Evaluar("");
        Assert.True(r.Pasa);
    }

    [Fact]
    public async Task Parse_JsonRoto_NoBloqueaConPasaPorDefecto()
    {
        // Llaves desbalanceadas: parsea entre la primera { y la última }, el contenido
        // intermedio es JSON inválido. La base devuelve Pasa=true con un mensaje.
        var r = await Evaluar("{ pasa: false, hallazgo: sin comillas }");
        Assert.True(r.Pasa);
        Assert.NotNull(r.Hallazgo); // mensaje informativo del fallback
    }

    [Fact]
    public async Task Parse_CamposEnBlanco_DevuelveNull()
    {
        var r = await Evaluar("""{ "pasa": false, "hallazgo": "  ", "parche": "" }""");
        Assert.False(r.Pasa);
        Assert.Null(r.Hallazgo);
        Assert.Null(r.Parche);
    }
}
