using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Data.Repositories;

public interface IRegistroPasoRepository
{
    Task AppendAsync(RegistroPaso paso, CancellationToken ct = default);
    Task<IReadOnlyList<RegistroPaso>> ListPorObraAsync(Guid obraId, CancellationToken ct = default);
}

public sealed class RegistroPasoRepository : IRegistroPasoRepository
{
    private readonly NovelaDbContext _db;
    public RegistroPasoRepository(NovelaDbContext db) => _db = db;

    public async Task AppendAsync(RegistroPaso paso, CancellationToken ct = default) =>
        await _db.RegistrosPaso.AddAsync(paso, ct);

    public async Task<IReadOnlyList<RegistroPaso>> ListPorObraAsync(Guid obraId, CancellationToken ct = default) =>
        await _db.RegistrosPaso
            .AsNoTracking()
            .Where(p => p.ObraId == obraId)
            .OrderBy(p => p.Timestamp)
            .ToListAsync(ct);
}
