using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Data.Repositories;

public interface IObraRepository
{
    Task<Obra?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Obra?> GetConCanonAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Obra>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Obra obra, CancellationToken ct = default);
    void Remove(Obra obra);
    // Soft delete: marca EliminadaEn. La obra deja de aparecer en consultas normales.
    Task<bool> EliminarAsync(Guid id, CancellationToken ct = default);
    Task<bool> RestaurarAsync(Guid id, CancellationToken ct = default);
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

    public async Task<bool> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var obra = await _db.Obras.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (obra is null) return false;
        obra.EliminadaEn = DateTimeOffset.UtcNow;
        return true;
    }

    // IgnoreQueryFilters porque la obra está oculta por el filtro de soft delete.
    public async Task<bool> RestaurarAsync(Guid id, CancellationToken ct = default)
    {
        var obra = await _db.Obras.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (obra is null) return false;
        obra.EliminadaEn = null;
        return true;
    }
}
