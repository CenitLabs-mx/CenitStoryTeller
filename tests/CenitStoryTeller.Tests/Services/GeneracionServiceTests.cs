using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Services;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Services;

public class GeneracionServiceTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    private (Guid obraId, Guid capituloId) Seedear(Action<Obra>? extra = null)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "T", Intake = IntakeTipo.Idea };
        var cap = new Capitulo { Id = Guid.NewGuid(), Titulo = "Cap 1", Orden = 1, ObraId = obra.Id };
        obra.Capitulos.Add(cap);
        extra?.Invoke(obra);
        seed.Obras.Add(obra);
        seed.SaveChanges();
        return (obra.Id, cap.Id);
    }

    private (GeneracionService svc, FakeLlmClient llm, TestDb.Suite suite)
        Servicio(IAcidTestRunner? runner = null)
    {
        var suite = _test.NuevaSuite();
        var llm = new FakeLlmClient();
        var factory = new FakeLlmClientFactory(llm);
        var opts = new FakeLlmOptionsAccessor
        {
            Options = new LlmOptions { ModelDraft = "draft-m", ModelReview = "review-m" }
        };
        var svc = new GeneracionService(factory, opts, new FakePromptProvider(),
            suite.Capitulos, suite.Obras, suite.Pasos, suite.Uow,
            runner ?? new NoopAcidRunner());
        return (svc, llm, suite);
    }

    private sealed class NoopAcidRunner : IAcidTestRunner
    {
        public Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(
            AcidContext ctx, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DimensionResultado>>(Array.Empty<DimensionResultado>());
    }

    [Fact]
    public async Task GenerarBorrador_PersisteVersionConNumero1YRegistraPaso()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("prosa generada", model: "draft-m");

        var v = await svc.GenerarBorradorAsync(capId, "prompt usuario");

        Assert.Equal(1, v.NumeroVersion);
        Assert.Equal("draft-m", v.Modelo);
        Assert.Equal("prosa generada", v.Texto);
        Assert.False(v.EsFinal);
        Assert.Equal("draft-m", llm.Requests[0].Model);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task GenerarBorrador_SegundaLlamada_Incrementa_NumeroVersion()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("v1").EncolarTexto("v2");

        var v1 = await svc.GenerarBorradorAsync(capId, "p1");
        var v2 = await svc.GenerarBorradorAsync(capId, "p2");

        Assert.Equal(1, v1.NumeroVersion);
        Assert.Equal(2, v2.NumeroVersion);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task GenerarBorrador_CapituloInexistente_Lanza()
    {
        Seedear();
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("no-importa");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.GenerarBorradorAsync(Guid.NewGuid(), "p"));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task CorrerPruebaAcido_ParseaVeredictoYActualizaFlags()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("prosa");
        var version = await svc.GenerarBorradorAsync(capId, "p");

        llm.EncolarTexto("""
            { "fisica": true, "psicologica": true, "ambiental": true, "quimica": true,
              "veredicto": "Aprobado", "hallazgos": null, "parches": null }
        """);

        var prueba = await svc.CorrerPruebaAcidoAsync(version.Id);

        Assert.Equal(Veredicto.Aprobado, prueba.Veredicto);
        Assert.True(prueba.Fisica);
        Assert.True(prueba.Psicologica);

        var reviewReq = llm.Requests[1];
        Assert.Equal("review-m", reviewReq.Model);
        Assert.Equal(0.2, reviewReq.Temperature);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task CorrerPruebaAcido_VeredictoRevisar_DisparaRunnerDimensionalYFusiona()
    {
        var runner = new GrabableAcidRunner(
            new DimensionResultado(AcidDimension.Fisica, new AcidResult(false, "fallo físico", "parche físico")),
            new DimensionResultado(AcidDimension.Psicologica, new AcidResult(true, null, null)));

        var (_, capId) = Seedear(obra =>
        {
            obra.Personajes.Add(new Personaje { Nombre = "Ana" });
            obra.Ubicaciones.Add(new Ubicacion { Nombre = "Plaza" });
        });
        var (svc, llm, suite) = Servicio(runner);

        llm.EncolarTexto("prosa");
        var version = await svc.GenerarBorradorAsync(capId, "p");

        llm.EncolarTexto("""
            { "fisica": false, "psicologica": true, "ambiental": true, "quimica": true,
              "veredicto": "Revisar", "hallazgos": "global", "parches": "global" }
        """);

        var prueba = await svc.CorrerPruebaAcidoAsync(version.Id);

        Assert.Equal(1, runner.Invocaciones);
        Assert.False(prueba.Fisica);
        Assert.True(prueba.Psicologica);
        Assert.Contains("[Fisica] fallo físico", prueba.Hallazgos);
        Assert.Contains("[Fisica] parche físico", prueba.Parches);

        suite.Db.Dispose();
    }

    private sealed class GrabableAcidRunner : IAcidTestRunner
    {
        private readonly IReadOnlyList<DimensionResultado> _r;
        public int Invocaciones { get; private set; }
        public GrabableAcidRunner(params DimensionResultado[] r) => _r = r;
        public Task<IReadOnlyList<DimensionResultado>> EjecutarAsync(
            AcidContext ctx, CancellationToken ct = default)
        {
            Invocaciones++;
            return Task.FromResult(_r);
        }
    }

    // ---- GenerarBorradoresPendientesAsync ----

    private Guid SeedearObraConCapitulos(int cantidad, Action<int, Capitulo>? hookExtra = null)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "T", Intake = IntakeTipo.Idea };
        for (var i = 1; i <= cantidad; i++)
        {
            var cap = new Capitulo
            {
                Id = Guid.NewGuid(),
                Titulo = $"Cap {i}",
                Orden = i,
                ObraId = obra.Id,
                Estado = CapituloEstado.Esquema
            };
            hookExtra?.Invoke(i, cap);
            obra.Capitulos.Add(cap);
        }
        seed.Obras.Add(obra);
        seed.SaveChanges();
        return obra.Id;
    }

    [Fact]
    public async Task GenerarBorradoresPendientes_GeneraUnaVersionPorCapitulo()
    {
        var obraId = SeedearObraConCapitulos(3);
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("prosa 1").EncolarTexto("prosa 2").EncolarTexto("prosa 3");

        var creados = await svc.GenerarBorradoresPendientesAsync(obraId);

        Assert.Equal(3, creados);
        Assert.Equal(3, suite.Db.CapituloVersiones.Count());

        suite.Db.Dispose();
    }

    [Fact]
    public async Task GenerarBorradoresPendientes_OmiteCapitulosConVersionExistente()
    {
        // Tres capítulos; al primero le pre-cargamos una versión. Solo se deben
        // generar 2 borradores nuevos.
        var obraId = SeedearObraConCapitulos(3);
        var primerCap = _test.NuevoCtx().Capitulos.OrderBy(c => c.Orden).First();
        using (var pre = _test.NuevoCtx())
        {
            pre.CapituloVersiones.Add(new CapituloVersion
            {
                Id = Guid.NewGuid(),
                CapituloId = primerCap.Id,
                NumeroVersion = 1,
                Modelo = "pre",
                Texto = "ya existía",
                CreadoEn = DateTimeOffset.UtcNow
            });
            pre.SaveChanges();
        }

        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("prosa 2").EncolarTexto("prosa 3");

        var creados = await svc.GenerarBorradoresPendientesAsync(obraId);

        Assert.Equal(2, creados);
        Assert.Equal(3, suite.Db.CapituloVersiones.Count()); // 1 pre + 2 nuevos
        Assert.Equal(2, llm.Requests.Count);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task GenerarBorradoresPendientes_ReportaProgresoPorCapitulo()
    {
        var obraId = SeedearObraConCapitulos(2);
        var (svc, llm, suite) = Servicio();
        llm.EncolarTexto("p1").EncolarTexto("p2");

        var reportes = new List<BorradorProgreso>();
        var progress = new Progress<BorradorProgreso>(p => reportes.Add(p));

        await svc.GenerarBorradoresPendientesAsync(obraId, progress);

        // Un Report al iniciar cada cap + uno al terminar todo => 3 reportes mínimos.
        // Progress<T> postea en el SynchronizationContext capturado; en xunit con
        // ExecutionContext fluyendo, los reportes llegan antes de que await retorne.
        Assert.True(reportes.Count >= 2, $"Esperaba al menos 2 reportes, llegaron {reportes.Count}");
        Assert.Contains(reportes, r => r.CapituloActual == "Cap 1");
        Assert.Contains(reportes, r => r.CapituloActual == "Cap 2");
        Assert.Contains(reportes, r => r.Hecho == r.Total && r.Total == 2);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task GenerarBorradoresPendientes_SinPendientes_DevuelveCero()
    {
        var obraId = SeedearObraConCapitulos(0);
        var (svc, _, suite) = Servicio();

        var creados = await svc.GenerarBorradoresPendientesAsync(obraId);

        Assert.Equal(0, creados);

        suite.Db.Dispose();
    }

    // ---- RegenerarConObservacionesAsync ----

    [Fact]
    public async Task Regenerar_CreaNuevaVersion_ConNumeroIncrementado()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();

        // v1 inicial.
        llm.EncolarTexto("prosa v1");
        var v1 = await svc.GenerarBorradorAsync(capId, "p");

        // Regeneración → v2.
        llm.EncolarTexto("prosa v2 corregida");
        var v2 = await svc.RegenerarConObservacionesAsync(v1.Id, "Ana está muerta pero camina. Corregir.");

        Assert.Equal(2, v2.NumeroVersion);
        Assert.Equal(v1.CapituloId, v2.CapituloId);
        Assert.Equal("prosa v2 corregida", v2.Texto);
        Assert.False(v2.EsFinal);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Regenerar_IncluyeTextoAnteriorYObservacionesEnElPrompt()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("la prosa que falló");
        var v1 = await svc.GenerarBorradorAsync(capId, "p");

        llm.EncolarTexto("regenerada");
        var observaciones = "OBSERVACIÓN ÚNICA: ajustar el farol";
        await svc.RegenerarConObservacionesAsync(v1.Id, observaciones);

        // La segunda request debe contener tanto el texto anterior como la observación.
        var requestRegeneracion = llm.Requests[1];
        var userMsg = requestRegeneracion.Messages.Single(m => m.Role == LlmRole.User).Content;
        Assert.Contains("la prosa que falló", userMsg);
        Assert.Contains(observaciones, userMsg);

        // Usa ModelDraft (es regenerar prosa, no validar).
        Assert.Equal("draft-m", requestRegeneracion.Model);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Regenerar_ObservacionesVacias_Lanza()
    {
        var (_, capId) = Seedear();
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("v1");
        var v1 = await svc.GenerarBorradorAsync(capId, "p");

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.RegenerarConObservacionesAsync(v1.Id, "   "));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Regenerar_VersionInexistente_Lanza()
    {
        var (svc, _, suite) = Servicio();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RegenerarConObservacionesAsync(Guid.NewGuid(), "obs"));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Regenerar_RegistraPasoAuditable()
    {
        var (obraId, capId) = Seedear();
        var (svc, llm, suite) = Servicio();

        llm.EncolarTexto("v1");
        var v1 = await svc.GenerarBorradorAsync(capId, "p");

        llm.EncolarTexto("v2");
        await svc.RegenerarConObservacionesAsync(v1.Id, "ajustar todo");

        // Hay 2 pasos: el de GenerarBorrador y el de Regenerar.
        var pasos = suite.Db.RegistrosPaso.Where(r => r.ObraId == obraId).ToList();
        Assert.Equal(2, pasos.Count);
        Assert.Contains(pasos, p => p.Accion.Contains("Regeneración con observaciones"));

        suite.Db.Dispose();
    }
}
