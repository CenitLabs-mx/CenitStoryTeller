using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Data.Services;

// Inserción y reordenamiento de beats. Mantiene Orden contiguo (1..N) en cada
// operación: insertar en posición K corre Orden+1 a todos los Orden >= K antes
// de añadir el nuevo; mover intercambia Orden con el vecino.
public interface IBeatService
{
    Task<Beat> InsertarEnPosicionAsync(
        Guid obraId, int posicion, Beat nuevo, CancellationToken ct = default);

    // delta = -1 (sube) | +1 (baja). Si el beat ya está en el extremo, no-op.
    Task<bool> MoverAsync(Guid beatId, int delta, CancellationToken ct = default);
}

public sealed class BeatService : IBeatService
{
    private readonly NovelaDbContext _db;
    private readonly IUnitOfWork _uow;

    public BeatService(NovelaDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<Beat> InsertarEnPosicionAsync(
        Guid obraId, int posicion, Beat nuevo, CancellationToken ct = default)
    {
        if (posicion < 1)
            throw new ArgumentOutOfRangeException(nameof(posicion), "La posición debe ser >= 1.");

        var existentes = await _db.Beats
            .Where(b => b.ObraId == obraId)
            .OrderBy(b => b.Orden)
            .ToListAsync(ct);

        var clamped = Math.Min(posicion, existentes.Count + 1);

        foreach (var b in existentes.Where(b => b.Orden >= clamped))
            b.Orden++;

        // Capítulos mantienen alineación con beats por Orden — corremos también
        // los suyos para que el cap nuevo (que creamos abajo) ocupe el hueco.
        var capsExistentes = await _db.Capitulos
            .Where(c => c.ObraId == obraId)
            .ToListAsync(ct);
        foreach (var c in capsExistentes.Where(c => c.Orden >= clamped))
            c.Orden++;

        nuevo.ObraId = obraId;
        nuevo.Orden = clamped;
        await _db.Beats.AddAsync(nuevo, ct);

        await _db.Capitulos.AddAsync(new Capitulo
        {
            ObraId = obraId,
            Orden = clamped,
            Titulo = nuevo.Titulo,
            Estado = CapituloEstado.Borrador
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return nuevo;
    }

    public async Task<bool> MoverAsync(Guid beatId, int delta, CancellationToken ct = default)
    {
        if (delta != -1 && delta != 1)
            throw new ArgumentOutOfRangeException(nameof(delta), "delta debe ser -1 o +1.");

        var beat = await _db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null) return false;

        var nuevoOrden = beat.Orden + delta;
        var vecino = await _db.Beats
            .FirstOrDefaultAsync(b => b.ObraId == beat.ObraId && b.Orden == nuevoOrden, ct);
        if (vecino is null) return false; // ya está en el extremo

        // Intercambio. Sin colisión transitoria porque cada beat tiene su propia
        // fila — el orden de SaveChanges aplica ambas actualizaciones a la vez.
        (beat.Orden, vecino.Orden) = (vecino.Orden, beat.Orden);

        // Mismo intercambio en capítulos si están alineados por Orden.
        var capBeat = await _db.Capitulos
            .FirstOrDefaultAsync(c => c.ObraId == beat.ObraId && c.Orden == vecino.Orden, ct);
        var capVecino = await _db.Capitulos
            .FirstOrDefaultAsync(c => c.ObraId == beat.ObraId && c.Orden == beat.Orden, ct);
        if (capBeat is not null && capVecino is not null)
            (capBeat.Orden, capVecino.Orden) = (capVecino.Orden, capBeat.Orden);

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
