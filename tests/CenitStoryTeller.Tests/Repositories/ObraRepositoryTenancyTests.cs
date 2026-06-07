using System;
using System.Linq;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Repositories;

public class ObraRepositoryTenancyTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    private static Guid GuidUserA => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static Guid GuidUserB => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private void Seedear()
    {
        // Seed con un DbContext anónimo (sin filtros) para meter datos de todos.
        using var seed = _test.NuevoCtx();
        seed.Obras.AddRange(
            new Obra { Id = Guid.NewGuid(), Titulo = "Demo", Intake = IntakeTipo.DominioPublico, UsuarioId = null },
            new Obra { Id = Guid.NewGuid(), Titulo = "ObraA", Intake = IntakeTipo.Idea, UsuarioId = GuidUserA },
            new Obra { Id = Guid.NewGuid(), Titulo = "ObraB", Intake = IntakeTipo.Idea, UsuarioId = GuidUserB });
        seed.SaveChanges();
    }

    [Fact]
    public async Task UsuarioA_VeSusObras_YDemos_NoLasDeB()
    {
        Seedear();
        var suite = _test.NuevaSuite(StubCurrentUser.De(GuidUserA));

        var lista = await suite.Obras.ListAsync();
        var titulos = lista.Select(o => o.Titulo).OrderBy(t => t).ToArray();

        Assert.Equal(new[] { "Demo", "ObraA" }, titulos);
    }

    [Fact]
    public async Task UsuarioB_VeSusObras_YDemos_NoLasDeA()
    {
        Seedear();
        var suite = _test.NuevaSuite(StubCurrentUser.De(GuidUserB));

        var lista = await suite.Obras.ListAsync();
        var titulos = lista.Select(o => o.Titulo).OrderBy(t => t).ToArray();

        Assert.Equal(new[] { "Demo", "ObraB" }, titulos);
    }

    [Fact]
    public async Task Anonimo_SoloVeDemos()
    {
        Seedear();
        var suite = _test.NuevaSuite(StubCurrentUser.Anonymous());

        var lista = await suite.Obras.ListAsync();

        Assert.Single(lista);
        Assert.Equal("Demo", lista[0].Titulo);
    }

    [Fact]
    public async Task NuevaObra_EstampaUsuarioIdAutomaticamente()
    {
        var suite = _test.NuevaSuite(StubCurrentUser.De(GuidUserA));
        var obra = new Obra { Titulo = "Nueva", Intake = IntakeTipo.Idea };

        await suite.Obras.AddAsync(obra);
        await suite.Uow.SaveChangesAsync();

        Assert.Equal(GuidUserA, obra.UsuarioId);
    }

    [Fact]
    public async Task NuevaObra_AnonimoQuedaComoDemo()
    {
        var suite = _test.NuevaSuite(StubCurrentUser.Anonymous());
        var obra = new Obra { Titulo = "Nueva", Intake = IntakeTipo.Idea };

        await suite.Obras.AddAsync(obra);
        await suite.Uow.SaveChangesAsync();

        Assert.Null(obra.UsuarioId);
    }

    [Fact]
    public async Task DemoEsReadOnly_NoSePuedeEliminar()
    {
        Seedear();
        var demoSuite = _test.NuevaSuite(StubCurrentUser.De(GuidUserA));
        var demoObraId = (await demoSuite.Obras.ListAsync())
            .Single(o => o.Titulo == "Demo").Id;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => demoSuite.Obras.EliminarAsync(demoObraId));
    }

    [Fact]
    public async Task UsuarioA_NoPuedeMutarObraDeB()
    {
        Seedear();
        // Suite con filtro desactivado (consultamos como B para conseguir el id),
        // luego cambiamos al usuario A para intentar eliminarla.
        Guid obraBId;
        using (var ctxB = _test.NuevoCtx(StubCurrentUser.De(GuidUserB)))
            obraBId = ctxB.Obras.Single(o => o.Titulo == "ObraB").Id;

        // El repo de A no encuentra la obra de B por el filtro, así que EliminarAsync
        // devuelve false (no lanza) — comportamiento equivalente a "no existe para mí".
        var suiteA = _test.NuevaSuite(StubCurrentUser.De(GuidUserA));
        var resultado = await suiteA.Obras.EliminarAsync(obraBId);

        Assert.False(resultado);
    }
}
