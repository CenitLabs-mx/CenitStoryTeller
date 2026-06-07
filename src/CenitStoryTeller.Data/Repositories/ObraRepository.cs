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
    private readonly ICurrentUser _currentUser;
    public ObraRepository(NovelaDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

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

    // Nueva obra: estampa UsuarioId con el usuario autenticado actual. Si no hay
    // usuario (background, --migrate, seeder), queda NULL = obra demo.
    public async Task AddAsync(Obra obra, CancellationToken ct = default)
    {
        if (obra.UsuarioId is null && _currentUser.Id is Guid uid)
            obra.UsuarioId = uid;
        await _db.Obras.AddAsync(obra, ct);
    }

    public void Remove(Obra obra)
    {
        AssertMutable(obra);
        _db.Obras.Remove(obra);
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var obra = await _db.Obras.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (obra is null) return false;
        AssertMutable(obra);
        obra.EliminadaEn = DateTimeOffset.UtcNow;
        return true;
    }

    // IgnoreQueryFilters porque la obra está oculta por el filtro de soft delete.
    public async Task<bool> RestaurarAsync(Guid id, CancellationToken ct = default)
    {
        var obra = await _db.Obras.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (obra is null) return false;
        AssertMutable(obra);
        obra.EliminadaEn = null;
        return true;
    }

    // Las obras demo (UsuarioId == null) son de solo lectura para cualquier usuario.
    // Solo el dueño puede mutar; el resto recibe UnauthorizedAccessException.
    private void AssertMutable(Obra obra)
    {
        if (obra.UsuarioId is null)
            throw new UnauthorizedAccessException("Las obras demo son de solo lectura.");
        if (obra.UsuarioId != _currentUser.Id)
            throw new UnauthorizedAccessException("Esta obra pertenece a otro usuario.");
    }
}
