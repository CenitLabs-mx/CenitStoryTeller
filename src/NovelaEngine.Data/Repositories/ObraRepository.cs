using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Data.Repositories;

public interface IObraRepository
{
    Task<Obra?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Obra?> GetConCanonAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Obra>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Obra obra, CancellationToken ct = default);
    void Remove(Obra obra);
}

public sealed class ObraRepository : IObraRepository
{
    private readonly NovelaDbContext _db;
    public ObraRepository(NovelaDbContext db) => _db = db;

    public Task<Obra?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Obras.FirstOrDefaultAsync(o => o.Id == id, ct);

    // Carga el canon completo de una obra (para alimentar el contexto de los agentes).
    public Task<Obra?> GetConCanonAsync(Guid id, CancellationToken ct = default) =>
        _db.Obras
            .Include(o => o.Personajes)
            .Include(o => o.Ubicaciones)
            .Include(o => o.Beats)
            .Include(o => o.Eventos).ThenInclude(e => e.Personajes)
            .Include(o => o.Eventos).ThenInclude(e => e.Ubicaciones)
            .Include(o => o.Capitulos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Obra>> ListAsync(CancellationToken ct = default) =>
        await _db.Obras.AsNoTracking().OrderBy(o => o.Titulo).ToListAsync(ct);

    public async Task AddAsync(Obra obra, CancellationToken ct = default) =>
        await _db.Obras.AddAsync(obra, ct);

    public void Remove(Obra obra) => _db.Obras.Remove(obra);
}
