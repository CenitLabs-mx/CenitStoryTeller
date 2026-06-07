using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Data.Entities;

namespace CenitStoryTeller.Data.Repositories;

public interface IConfiguracionLlmRepository
{
    Task<ConfiguracionLlm?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    // Upsert: si ya existe para ese usuario, la actualiza; si no, la crea.
    Task GuardarAsync(ConfiguracionLlm config, CancellationToken ct = default);
}

public sealed class ConfiguracionLlmRepository : IConfiguracionLlmRepository
{
    private readonly CenitStoryTellerDbContext _db;
    public ConfiguracionLlmRepository(CenitStoryTellerDbContext db) => _db = db;

    public Task<ConfiguracionLlm?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        _db.ConfiguracionesLlm.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public async Task GuardarAsync(ConfiguracionLlm config, CancellationToken ct = default)
    {
        var actual = await _db.ConfiguracionesLlm
            .FirstOrDefaultAsync(c => c.UsuarioId == config.UsuarioId, ct);

        if (actual is null)
        {
            await _db.ConfiguracionesLlm.AddAsync(config, ct);
            return;
        }

        actual.Provider = config.Provider;
        actual.ApiKey = config.ApiKey;
        actual.BaseUrl = config.BaseUrl;
        actual.ModelDraft = config.ModelDraft;
        actual.ModelReview = config.ModelReview;
        actual.ActualizadaEn = DateTimeOffset.UtcNow;
    }
}
