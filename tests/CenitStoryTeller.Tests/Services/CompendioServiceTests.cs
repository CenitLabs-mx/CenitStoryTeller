using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data;
using CenitStoryTeller.Data.Services;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Services;

public class CompendioServiceTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    // Obra con 5 capítulos (Orden 1..5), 5 beats. Cada capítulo apunta a su beat
    // por Orden (sin BeatObjetivoId — el CompendioService no lo necesita).
    private Guid SeedearObraCon5(Action<NovelaDbContext, Obra>? versiones = null)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "T", Intake = IntakeTipo.Idea };
        for (var i = 1; i <= 5; i++)
        {
            obra.Beats.Add(new Beat
            {
                Id = Guid.NewGuid(), ObraId = obra.Id, Titulo = $"Beat {i}",
                Orden = i, Acto = Acto.Acto1, Funcion = FuncionNarrativa.Setup,
                Descripcion = $"desc {i}",
                Estado = BeatEstado.Pendiente
            });
            obra.Capitulos.Add(new Capitulo
            {
                Id = Guid.NewGuid(), ObraId = obra.Id, Titulo = $"Cap {i}",
                Orden = i, Estado = CapituloEstado.Borrador
            });
        }
        seed.Obras.Add(obra);
        seed.SaveChanges();
        versiones?.Invoke(seed, obra);
        seed.SaveChanges();
        return obra.Id;
    }

    private (CompendioService svc, FakeLlmClient llm, TestDb.Suite suite, IMemoryCache cache) Servicio()
    {
        var suite = _test.NuevaSuite();
        var llm = new FakeLlmClient();
        var factory = new FakeLlmClientFactory(llm);
        var opts = new FakeLlmOptionsAccessor
        {
            Options = new LlmOptions { ModelDraft = "draft-m", ModelReview = "review-m" }
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new CompendioService(suite.Obras, suite.Capitulos, factory, opts, cache);
        return (svc, llm, suite, cache);
    }

    [Fact]
    public async Task ObtenerAsync_SinFinalesPrevios_DevuelveMensajeYNoLlamaAlLlm()
    {
        var obraId = SeedearObraCon5();
        var (svc, llm, suite, _) = Servicio();

        var comp = await svc.ObtenerAsync(obraId, hastaOrden: 3);

        Assert.Contains("(no hay capítulos previos aprobados)", comp.Pasado);
        Assert.Empty(llm.Requests); // No se invocó al LLM porque no hay nada que resumir.

        suite.Db.Dispose();
    }

    [Fact]
    public async Task ObtenerAsync_PasadoIncluyeSoloVersionesFinalesAntesDeHastaOrden()
    {
        var obraId = SeedearObraCon5((ctx, obra) =>
        {
            // Aprobamos finales en los capítulos 1, 2 y 4. El cap 4 NO debe entrar
            // cuando pedimos hastaOrden=3.
            foreach (var ord in new[] { 1, 2, 4 })
            {
                var cap = ctx.Capitulos.Single(c => c.ObraId == obra.Id && c.Orden == ord);
                ctx.CapituloVersiones.Add(new CapituloVersion
                {
                    Id = Guid.NewGuid(), CapituloId = cap.Id, NumeroVersion = 1,
                    Modelo = "m", Texto = $"prosa final cap{ord}",
                    EsFinal = true, CreadoEn = DateTimeOffset.UtcNow
                });
            }
        });

        var (svc, llm, suite, _) = Servicio();
        llm.EncolarTexto("RESUMEN DEL PASADO");

        var comp = await svc.ObtenerAsync(obraId, hastaOrden: 3);

        Assert.Equal("RESUMEN DEL PASADO", comp.Pasado);
        // En el prompt que recibió el LLM solo deben aparecer los caps 1 y 2.
        var userMsg = llm.Requests[0].Messages.Single(m => m.Role == LlmRole.User).Content;
        Assert.Contains("Cap 1", userMsg);
        Assert.Contains("Cap 2", userMsg);
        Assert.DoesNotContain("Cap 4", userMsg);
        // Y debe usar ModelDraft (resumir es barato).
        Assert.Equal("draft-m", llm.Requests[0].Model);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task ObtenerAsync_FuturoListaBeatsPosterioresPendientes_SinLlamarAlLlm()
    {
        var obraId = SeedearObraCon5();
        var (svc, llm, suite, _) = Servicio();

        var comp = await svc.ObtenerAsync(obraId, hastaOrden: 3);

        // Pendientes con Orden > 3 son beats 4 y 5.
        Assert.Contains("Beat 4", comp.Futuro);
        Assert.Contains("Beat 5", comp.Futuro);
        Assert.DoesNotContain("Beat 1", comp.Futuro);
        Assert.DoesNotContain("Beat 3", comp.Futuro);
        // El futuro no requiere LLM.
        Assert.Empty(llm.Requests);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task ObtenerAsync_SegundaInvocacion_MismoEstadoUsaCache()
    {
        var obraId = SeedearObraCon5((ctx, obra) =>
        {
            var cap1 = ctx.Capitulos.Single(c => c.ObraId == obra.Id && c.Orden == 1);
            ctx.CapituloVersiones.Add(new CapituloVersion
            {
                Id = Guid.NewGuid(), CapituloId = cap1.Id, NumeroVersion = 1,
                Modelo = "m", Texto = "prosa final cap1",
                EsFinal = true, CreadoEn = DateTimeOffset.UtcNow
            });
        });

        var (svc, llm, suite, _) = Servicio();
        llm.EncolarTexto("RESUMEN");

        var primero = await svc.ObtenerAsync(obraId, hastaOrden: 2);
        var segundo = await svc.ObtenerAsync(obraId, hastaOrden: 2);

        Assert.Equal(primero.Pasado, segundo.Pasado);
        Assert.Single(llm.Requests); // Cache hit en la 2ª llamada → 1 sola al LLM.

        suite.Db.Dispose();
    }

    [Fact]
    public async Task ObtenerAsync_NuevoFinalAprobado_InvalidaCache()
    {
        var obraId = SeedearObraCon5((ctx, obra) =>
        {
            var cap1 = ctx.Capitulos.Single(c => c.ObraId == obra.Id && c.Orden == 1);
            ctx.CapituloVersiones.Add(new CapituloVersion
            {
                Id = Guid.NewGuid(), CapituloId = cap1.Id, NumeroVersion = 1,
                Modelo = "m", Texto = "prosa final cap1",
                EsFinal = true, CreadoEn = DateTimeOffset.UtcNow
            });
        });

        var (svc, llm, suite, _) = Servicio();
        llm.EncolarTexto("RESUMEN-A").EncolarTexto("RESUMEN-B");

        var primero = await svc.ObtenerAsync(obraId, hastaOrden: 3);

        // Aprobamos cap 2 (nuevo final) — la clave del cache cambia.
        using (var ctx2 = _test.NuevoCtx())
        {
            var cap2 = ctx2.Capitulos.Single(c => c.ObraId == obraId && c.Orden == 2);
            ctx2.CapituloVersiones.Add(new CapituloVersion
            {
                Id = Guid.NewGuid(), CapituloId = cap2.Id, NumeroVersion = 1,
                Modelo = "m", Texto = "prosa final cap2",
                EsFinal = true, CreadoEn = DateTimeOffset.UtcNow
            });
            ctx2.SaveChanges();
        }

        var segundo = await svc.ObtenerAsync(obraId, hastaOrden: 3);

        Assert.Equal("RESUMEN-A", primero.Pasado);
        Assert.Equal("RESUMEN-B", segundo.Pasado);
        Assert.Equal(2, llm.Requests.Count); // Cache miss en la 2ª → 2 llamadas.

        suite.Db.Dispose();
    }
}
