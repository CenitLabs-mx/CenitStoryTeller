using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Services;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Services;

public class ModernizacionServiceTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    private static ParametrosModernizacion Params() => new()
    {
        EpocaDestino = "2020s",
        LugarCultura = "Mexico urbano",
        Registro = "juvenil",
    };

    private Guid Seedear(IntakeTipo intake)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "Original", Intake = intake };
        seed.Obras.Add(obra);
        seed.SaveChanges();
        return obra.Id;
    }

    // Cada test recibe una Suite fresca (DbContext nuevo) para el servicio.
    private (ModernizacionService svc, FakeLlmClient llm, TestDb.Suite suite) Servicio()
    {
        var suite = _test.NuevaSuite();
        var llm = new FakeLlmClient();
        var svc = new ModernizacionService(
            llm,
            Options.Create(new LlmOptions { ModelDraft = "d", ModelReview = "r" }),
            new FakePromptProvider(),
            suite.Obras, suite.Pasos, suite.Uow);
        return (svc, llm, suite);
    }

    [Fact]
    public async Task Modernizar_IntakeNoDominioPublico_Lanza()
    {
        var obraId = Seedear(IntakeTipo.Idea);
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("no debería invocarse");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ModernizarCanonAsync(obraId, Params()));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Modernizar_ResuelveRelacionesNN_CaseInsensitive()
    {
        var obraId = Seedear(IntakeTipo.DominioPublico);
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("""
        {
          "personajes": [
            { "nombre": "Ana López", "rol": "protagonista", "estadoVital": "Vivo" },
            { "nombre": "Beto", "rol": "antagonista", "estadoVital": "Vivo" }
          ],
          "ubicaciones": [ { "nombre": "Plaza Central", "tipo": "exterior" } ],
          "beats": [ { "titulo": "B1", "orden": 1, "acto": "Acto1", "funcionNarrativa": "Setup" } ],
          "eventos": [
            {
              "titulo": "Choque", "orden": 1, "acto": "Acto1", "tipo": "Canonico",
              "personajes": [ "ANA LÓPEZ", "beto" ],
              "ubicaciones": [ "plaza central" ]
            }
          ]
        }
        """);

        var obra = await svc.ModernizarCanonAsync(obraId, Params());

        var evento = Assert.Single(obra.Eventos);
        Assert.Equal(2, evento.Personajes.Count);
        Assert.Single(evento.Ubicaciones);
        Assert.Contains(evento.Personajes, p => p.Nombre == "Ana López");
        Assert.Contains(evento.Personajes, p => p.Nombre == "Beto");

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Modernizar_NombreDesconocidoEnEvento_SeIgnoraEnVezDeFallar()
    {
        var obraId = Seedear(IntakeTipo.DominioPublico);
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("""
        {
          "personajes": [ { "nombre": "Ana", "estadoVital": "Vivo" } ],
          "ubicaciones": [],
          "beats": [],
          "eventos": [
            { "titulo": "X", "orden": 1, "tipo": "Canonico",
              "personajes": [ "Ana", "FantasmaInexistente" ], "ubicaciones": [] }
          ]
        }
        """);

        var obra = await svc.ModernizarCanonAsync(obraId, Params());
        var evento = Assert.Single(obra.Eventos);
        Assert.Single(evento.Personajes);
        Assert.Equal("Ana", evento.Personajes[0].Nombre);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Modernizar_MarcaCanonComoBorrador_YUsaModeloFuerte()
    {
        var obraId = Seedear(IntakeTipo.DominioPublico);
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("""
        {
          "personajes": [ { "nombre": "Ana", "estadoVital": "Vivo" } ],
          "ubicaciones": [ { "nombre": "Casa" } ],
          "beats": [],
          "eventos": []
        }
        """);

        var obra = await svc.ModernizarCanonAsync(obraId, Params());

        Assert.All(obra.Personajes, p => Assert.Equal(CanonNivel.Borrador, p.Canon));
        Assert.All(obra.Ubicaciones, u => Assert.Equal(CanonNivel.Borrador, u.Canon));
        Assert.Equal("r", llm.Requests[0].Model);

        suite.Db.Dispose();
    }
}
