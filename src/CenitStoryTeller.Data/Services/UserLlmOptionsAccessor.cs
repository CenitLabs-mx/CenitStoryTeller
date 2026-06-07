using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Data.Services;

// Devuelve LlmOptions específicas del usuario autenticado si tiene su propia config
// guardada; en cualquier otro caso (anonymous, sin fila, migrate, seed) cae a la
// configuración global de appsettings inyectada vía IOptions<LlmOptions>.
public sealed class UserLlmOptionsAccessor : ILlmOptionsAccessor
{
    private readonly ICurrentUser _currentUser;
    private readonly IConfiguracionLlmRepository _configRepo;
    private readonly LlmOptions _global;

    public UserLlmOptionsAccessor(
        ICurrentUser currentUser,
        IConfiguracionLlmRepository configRepo,
        IOptions<LlmOptions> global)
    {
        _currentUser = currentUser;
        _configRepo = configRepo;
        _global = global.Value;
    }

    public async Task<LlmOptions> ObtenerAsync(CancellationToken ct = default)
    {
        if (_currentUser.Id is not { } uid) return _global;

        var cfg = await _configRepo.ObtenerPorUsuarioAsync(uid, ct);
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.ApiKey)) return _global;

        return new LlmOptions
        {
            Provider = cfg.Provider,
            ApiKey = cfg.ApiKey,
            BaseUrl = cfg.BaseUrl,
            ModelDraft = cfg.ModelDraft,
            ModelReview = cfg.ModelReview,
            Temperature = _global.Temperature
        };
    }
}
