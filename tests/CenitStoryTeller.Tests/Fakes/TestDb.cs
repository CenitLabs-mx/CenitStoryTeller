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

    public NovelaDbContext NuevoCtx(ICurrentUser? user = null) => new(_opts, user);

    public sealed record Suite(
        NovelaDbContext Db,
        IObraRepository Obras,
        ICapituloRepository Capitulos,
        IRegistroPasoRepository Pasos,
        IConfiguracionLlmRepository ConfigLlm,
        IUnitOfWork Uow);

    // Construye una Suite (DbContext + repositorios) sobre un DbContext nuevo. Si no se
    // pasa usuario, se usa AnonymousCurrentUser (solo ve demos, no puede mutar).
    public Suite NuevaSuite(ICurrentUser? user = null)
    {
        var u = user ?? new AnonymousCurrentUser();
        var db = NuevoCtx(u);
        return new Suite(db,
            new ObraRepository(db, u),
            new CapituloRepository(db),
            new RegistroPasoRepository(db),
            new ConfiguracionLlmRepository(db),
            new UnitOfWork(db));
    }

    public void Dispose() => _conn.Dispose();
}
