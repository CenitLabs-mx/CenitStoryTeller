using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Data;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Tests.Fakes;

// Suite de tests respaldada por SQLite in-memory. Mantiene una conexión abierta (la BD vive
// con ella) y entrega DbContexts independientes para arrange y act — así el change tracker
// del seed no contamina al del servicio bajo prueba.
internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<NovelaDbContext> _opts;

    private TestDb(SqliteConnection conn, DbContextOptions<NovelaDbContext> opts)
    {
        _conn = conn;
        _opts = opts;
    }

    public static TestDb Crear()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<NovelaDbContext>().UseSqlite(conn).Options;

        using (var ctx = new NovelaDbContext(opts))
            ctx.Database.EnsureCreated();
        return new TestDb(conn, opts);
    }

    public NovelaDbContext NuevoCtx() => new(_opts);

    public sealed record Suite(
        NovelaDbContext Db,
        IObraRepository Obras,
        ICapituloRepository Capitulos,
        IRegistroPasoRepository Pasos,
        IUnitOfWork Uow);

    // Construye una Suite (DbContext + repositorios) sobre un DbContext nuevo. El llamador
    // debe disponer Suite.Db al terminar (o usar la TestDb completa con `using`).
    public Suite NuevaSuite()
    {
        var db = NuevoCtx();
        return new Suite(db,
            new ObraRepository(db),
            new CapituloRepository(db),
            new RegistroPasoRepository(db),
            new UnitOfWork(db));
    }

    public void Dispose() => _conn.Dispose();
}
