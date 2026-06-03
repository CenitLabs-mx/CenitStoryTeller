using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Data.Repositories;

public interface ICapituloRepository
{
    Task<Capitulo?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Capitulo?> GetConVersionesAsync(Guid id, CancellationToken ct = default);
    Task<CapituloVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default);
    Task<IReadOnlyList<Capitulo>> ListPorObraAsync(Guid obraId, CancellationToken ct = default);
    Task<int> SiguienteNumeroVersionAsync(Guid capituloId, CancellationToken ct = default);
    Task AgregarVersionAsync(CapituloVersion version, CancellationToken ct = default);
    Task GuardarPruebaAcidoAsync(PruebaAcido prueba, CancellationToken ct = default);
    Task MarcarFinalAsync(Guid versionId, CancellationToken ct = default);
}

public sealed class CapituloRepository : ICapituloRepository
{
    private readonly NovelaDbContext _db;
    public CapituloRepository(NovelaDbContext db) => _db = db;

    public Task<Capitulo?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Capitulos.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Capitulo?> GetConVersionesAsync(Guid id, CancellationToken ct = default) =>
        _db.Capitulos
            .Include(c => c.Versiones).ThenInclude(v => v.Acido)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<CapituloVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default) =>
        _db.CapituloVersiones
            .Include(v => v.Capitulo)
            .Include(v => v.Acido)
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

    public async Task<IReadOnlyList<Capitulo>> ListPorObraAsync(Guid obraId, CancellationToken ct = default) =>
        await _db.Capitulos.Where(c => c.ObraId == obraId).OrderBy(c => c.Orden).ToListAsync(ct);

    public async Task<int> SiguienteNumeroVersionAsync(Guid capituloId, CancellationToken ct = default)
    {
        var max = await _db.CapituloVersiones
            .Where(v => v.CapituloId == capituloId)
            .Select(v => (int?)v.NumeroVersion)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    public async Task AgregarVersionAsync(CapituloVersion version, CancellationToken ct = default) =>
        await _db.CapituloVersiones.AddAsync(version, ct);

    // Upsert de la prueba del ácido (1:1 con la versión).
    public async Task GuardarPruebaAcidoAsync(PruebaAcido prueba, CancellationToken ct = default)
    {
        var actual = await _db.PruebasAcido
            .FirstOrDefaultAsync(p => p.CapituloVersionId == prueba.CapituloVersionId, ct);

        if (actual is null)
        {
            await _db.PruebasAcido.AddAsync(prueba, ct);
            return;
        }

        actual.Fisica = prueba.Fisica;
        actual.Psicologica = prueba.Psicologica;
        actual.Ambiental = prueba.Ambiental;
        actual.Quimica = prueba.Quimica;
        actual.Anacronismo = prueba.Anacronismo;
        actual.FidelidadFuncional = prueba.FidelidadFuncional;
        actual.Veredicto = prueba.Veredicto;
        actual.Hallazgos = prueba.Hallazgos;
        actual.Parches = prueba.Parches;
    }

    // Marca una versión como final y desmarca las hermanas (operación atómica en BD).
    public async Task MarcarFinalAsync(Guid versionId, CancellationToken ct = default)
    {
        var capituloId = await _db.CapituloVersiones
            .Where(v => v.Id == versionId)
            .Select(v => (Guid?)v.CapituloId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No existe la versión {versionId}.");

        await _db.CapituloVersiones
            .Where(v => v.CapituloId == capituloId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.EsFinal, v => v.Id == versionId), ct);
    }
}
