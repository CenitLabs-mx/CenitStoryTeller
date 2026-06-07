using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Data.Repositories;

public interface IRegistroPasoRepository
{
    Task AppendAsync(RegistroPaso paso, CancellationToken ct = default);
    Task<List<RegistroPaso>> ListPorObraAsync(Guid obraId, CancellationToken ct = default);
}

public sealed class RegistroPasoRepository : IRegistroPasoRepository
{
    private readonly CenitStoryTellerDbContext _db;
    public RegistroPasoRepository(CenitStoryTellerDbContext db) => _db = db;

    public async Task AppendAsync(RegistroPaso paso, CancellationToken ct = default) =>
        await _db.RegistrosPaso.AddAsync(paso, ct);

    public Task<List<RegistroPaso>> ListPorObraAsync(Guid obraId, CancellationToken ct = default) =>
        _db.RegistrosPaso
            .Where(p => p.ObraId == obraId)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync(ct);
}
