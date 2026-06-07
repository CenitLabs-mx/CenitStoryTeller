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
}
