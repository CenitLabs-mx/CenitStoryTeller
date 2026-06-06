using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.AcidTests;

public class AcidTestRunnerTests
{
    private static AcidContext Ctx() => new(
        new Capitulo { Titulo = "C", Orden = 1 },
        "prosa",
        new List<Personaje> { new() { Nombre = "P" } },
        new Ubicacion { Nombre = "U" },
        new List<Evento>());

    // Test grabable: contamos invocaciones y resolvemos un Pasa/falla preconfigurado.
    private sealed class FakeTest : IAcidTest
    {
        public AcidDimension Dimension { get; }
        public bool Pasa { get; }
        public int Llamadas { get; private set; }

        public FakeTest(AcidDimension dim, bool pasa)
        {
            Dimension = dim;
            Pasa = pasa;
        }

        public Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
        {
            Llamadas++;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new AcidResult(Pasa, Pasa ? null : "h", Pasa ? null : "p"));
        }
    }

    [Fact]
    public async Task Ejecutar_PreservaOrdenDeRegistro()
    {
        // Damos los tests en un orden no alfabético para verificar que NO se ordena solo.
        var tests = new IAcidTest[]
        {
            new FakeTest(AcidDimension.Quimica, true),
            new FakeTest(AcidDimension.Fisica, false),
            new FakeTest(AcidDimension.Ambiental, true),
            new FakeTest(AcidDimension.Psicologica, true)
        };
        var runner = new AcidTestRunner(tests, new FakeLlmClient());

        var r = await runner.EjecutarAsync(Ctx());

        Assert.Equal(
            new[] { AcidDimension.Quimica, AcidDimension.Fisica, AcidDimension.Ambiental, AcidDimension.Psicologica },
            r.Select(x => x.Dimension));
    }

    [Fact]
    public async Task Ejecutar_MapeaResultadoPorDimension()
    {
        var fisica = new FakeTest(AcidDimension.Fisica, pasa: false);
        var ambiental = new FakeTest(AcidDimension.Ambiental, pasa: true);
        var runner = new AcidTestRunner(new IAcidTest[] { fisica, ambiental }, new FakeLlmClient());

        var r = await runner.EjecutarAsync(Ctx());

        Assert.False(r.Single(d => d.Dimension == AcidDimension.Fisica).Resultado.Pasa);
        Assert.True(r.Single(d => d.Dimension == AcidDimension.Ambiental).Resultado.Pasa);
    }

    [Fact]
    public async Task Ejecutar_PropagaCancelacion()
    {
        var t1 = new FakeTest(AcidDimension.Fisica, true);
        var t2 = new FakeTest(AcidDimension.Psicologica, true);
        var runner = new AcidTestRunner(new IAcidTest[] { t1, t2 }, new FakeLlmClient());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.EjecutarAsync(Ctx(), cts.Token));

        // Si se canceló al primer test, el segundo no debió ejecutarse.
        Assert.Equal(0, t2.Llamadas);
    }

    [Fact]
    public async Task Ejecutar_SinTests_DevuelveVacio()
    {
        var runner = new AcidTestRunner(Array.Empty<IAcidTest>(), new FakeLlmClient());
        var r = await runner.EjecutarAsync(Ctx());
        Assert.Empty(r);
    }
}
