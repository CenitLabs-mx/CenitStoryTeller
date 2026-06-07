using System;
using System.Linq;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Data.Services;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Services;

public class BeatServiceTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    // Sembrar una obra con N beats numerados 1..N.
    private Guid Seedear(int cantidad)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "T", Intake = IntakeTipo.Idea };
        for (var i = 1; i <= cantidad; i++)
        {
            obra.Beats.Add(new Beat
            {
                Id = Guid.NewGuid(), ObraId = obra.Id, Titulo = $"B{i}",
                Orden = i, Acto = Acto.Acto1, Funcion = FuncionNarrativa.Setup,
                Estado = BeatEstado.Pendiente
            });
        }
        seed.Obras.Add(obra);
        seed.SaveChanges();
        return obra.Id;
    }

    private (BeatService svc, TestDb.Suite suite) Servicio()
    {
        var suite = _test.NuevaSuite();
        return (new BeatService(suite.Db, suite.Uow), suite);
    }

    private List<(string Titulo, int Orden)> BeatsDe(Guid obraId)
    {
        using var ctx = _test.NuevoCtx();
        return ctx.Beats.Where(b => b.ObraId == obraId)
            .OrderBy(b => b.Orden)
            .Select(b => new ValueTuple<string, int>(b.Titulo, b.Orden))
            .ToList();
    }

    [Fact]
    public async Task Insertar_EnMedio_DesplazaPosteriores()
    {
        var obraId = Seedear(3);                 // B1=1, B2=2, B3=3
        var (svc, suite) = Servicio();

        var nuevo = new Beat
        {
            Titulo = "NUEVO", Acto = Acto.Acto1,
            Funcion = FuncionNarrativa.Detonante, Estado = BeatEstado.Pendiente
        };
        await svc.InsertarEnPosicionAsync(obraId, posicion: 2, nuevo);

        var resultado = BeatsDe(obraId);
        Assert.Equal(new[]
        {
            ("B1",    1),
            ("NUEVO", 2),
            ("B2",    3),
            ("B3",    4),
        }, resultado);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Insertar_AlPrincipio_DesplazaTodos()
    {
        var obraId = Seedear(2);
        var (svc, suite) = Servicio();

        await svc.InsertarEnPosicionAsync(obraId, 1, new Beat { Titulo = "X", Estado = BeatEstado.Pendiente });

        var resultado = BeatsDe(obraId);
        Assert.Equal(new[] { ("X", 1), ("B1", 2), ("B2", 3) }, resultado);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Insertar_PosicionMasAllaDelFinal_QuedaAlFinal()
    {
        var obraId = Seedear(2);
        var (svc, suite) = Servicio();

        // Pedimos posición 99 con solo 2 beats existentes — queda en la 3.
        await svc.InsertarEnPosicionAsync(obraId, 99, new Beat { Titulo = "ULTIMO", Estado = BeatEstado.Pendiente });

        var resultado = BeatsDe(obraId);
        Assert.Equal(new[] { ("B1", 1), ("B2", 2), ("ULTIMO", 3) }, resultado);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Mover_HaciaArriba_IntercambiaConVecino()
    {
        var obraId = Seedear(3);
        var (svc, suite) = Servicio();
        var b3 = suite.Db.Beats.Single(b => b.ObraId == obraId && b.Titulo == "B3");

        var ok = await svc.MoverAsync(b3.Id, -1);

        Assert.True(ok);
        Assert.Equal(new[] { ("B1", 1), ("B3", 2), ("B2", 3) }, BeatsDe(obraId));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Mover_HaciaAbajo_IntercambiaConVecino()
    {
        var obraId = Seedear(3);
        var (svc, suite) = Servicio();
        var b1 = suite.Db.Beats.Single(b => b.ObraId == obraId && b.Titulo == "B1");

        var ok = await svc.MoverAsync(b1.Id, +1);

        Assert.True(ok);
        Assert.Equal(new[] { ("B2", 1), ("B1", 2), ("B3", 3) }, BeatsDe(obraId));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Mover_EnExtremo_NoOp()
    {
        var obraId = Seedear(3);
        var (svc, suite) = Servicio();
        var b1 = suite.Db.Beats.Single(b => b.ObraId == obraId && b.Titulo == "B1");
        var b3 = suite.Db.Beats.Single(b => b.ObraId == obraId && b.Titulo == "B3");

        var subirArriba = await svc.MoverAsync(b1.Id, -1);
        var bajarAbajo  = await svc.MoverAsync(b3.Id, +1);

        Assert.False(subirArriba);
        Assert.False(bajarAbajo);
        Assert.Equal(new[] { ("B1", 1), ("B2", 2), ("B3", 3) }, BeatsDe(obraId));

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Mover_BeatInexistente_DevuelveFalse()
    {
        Seedear(2);
        var (svc, suite) = Servicio();

        var ok = await svc.MoverAsync(Guid.NewGuid(), -1);
        Assert.False(ok);

        suite.Db.Dispose();
    }

    // Variante que también siembra un capítulo por cada beat alineado por Orden.
    private Guid SeedearConCapitulos(int cantidad)
    {
        using var seed = _test.NuevoCtx();
        var obra = new Obra { Id = Guid.NewGuid(), Titulo = "T", Intake = IntakeTipo.Idea };
        for (var i = 1; i <= cantidad; i++)
        {
            obra.Beats.Add(new Beat
            {
                Id = Guid.NewGuid(), ObraId = obra.Id, Titulo = $"B{i}",
                Orden = i, Acto = Acto.Acto1, Funcion = FuncionNarrativa.Setup,
                Estado = BeatEstado.Pendiente
            });
            obra.Capitulos.Add(new Capitulo
            {
                Id = Guid.NewGuid(), ObraId = obra.Id,
                Titulo = $"Cap {i}", Orden = i, Estado = CapituloEstado.Borrador
            });
        }
        seed.Obras.Add(obra);
        seed.SaveChanges();
        return obra.Id;
    }

    [Fact]
    public async Task Insertar_TambienCreaCapituloYRenumeraCapitulosPosteriores()
    {
        var obraId = SeedearConCapitulos(3);
        var (svc, suite) = Servicio();

        await svc.InsertarEnPosicionAsync(obraId, 2,
            new Beat { Titulo = "NUEVO", Estado = BeatEstado.Pendiente });

        using var ctx = _test.NuevoCtx();
        var caps = ctx.Capitulos.Where(c => c.ObraId == obraId)
            .OrderBy(c => c.Orden)
            .Select(c => new ValueTuple<string, int>(c.Titulo, c.Orden))
            .ToList();
        Assert.Equal(new[]
        {
            ("Cap 1", 1),
            ("NUEVO", 2),     // capítulo nuevo creado con el título del beat
            ("Cap 2", 3),
            ("Cap 3", 4),
        }, caps);

        suite.Db.Dispose();
    }

    [Fact]
    public async Task Mover_TambienIntercambiaCapitulosAlineados()
    {
        var obraId = SeedearConCapitulos(3);
        var (svc, suite) = Servicio();
        var b1 = suite.Db.Beats.Single(b => b.ObraId == obraId && b.Titulo == "B1");

        await svc.MoverAsync(b1.Id, +1);

        using var ctx = _test.NuevoCtx();
        var caps = ctx.Capitulos.Where(c => c.ObraId == obraId)
            .OrderBy(c => c.Orden)
            .Select(c => c.Titulo)
            .ToList();
        // Cap 2 sube al Orden 1, Cap 1 baja al 2 — espejo del swap de beats.
        Assert.Equal(new[] { "Cap 2", "Cap 1", "Cap 3" }, caps);

        suite.Db.Dispose();
    }
}
