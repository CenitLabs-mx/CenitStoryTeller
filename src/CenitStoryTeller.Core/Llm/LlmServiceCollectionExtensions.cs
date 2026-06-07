using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CenitStoryTeller.Core.Llm;

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection AddCenitStoryTellerLlm(this IServiceCollection services, IConfiguration config)
    {
        // LlmOptions desde appsettings sigue siendo el fallback. La capa Data registra
        // un ILlmOptionsAccessor que prefiere la config del usuario; este accessor
        // por defecto devuelve la global cuando no hay nada mejor.
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddHttpClient();

        services.AddScoped<ILlmOptionsAccessor, AppsettingsLlmOptionsAccessor>();
        services.AddScoped<ILlmClientFactory, LlmClientFactory>();
        return services;
    }
}

// Fallback: devuelve la LlmOptions global de appsettings, sin importar el usuario.
// Útil para tests, --migrate, seeders. La capa Web la sobrescribe con la versión
// que mira la tabla ConfiguracionLlm.
internal sealed class AppsettingsLlmOptionsAccessor : ILlmOptionsAccessor
{
    private readonly LlmOptions _global;
    public AppsettingsLlmOptionsAccessor(IOptions<LlmOptions> opt) => _global = opt.Value;
    public Task<LlmOptions> ObtenerAsync(CancellationToken ct = default) => Task.FromResult(_global);
}

internal sealed class LlmClientFactory : ILlmClientFactory
{
    private readonly ILlmOptionsAccessor _accessor;
    private readonly IHttpClientFactory _http;

    public LlmClientFactory(ILlmOptionsAccessor accessor, IHttpClientFactory http)
    {
        _accessor = accessor;
        _http = http;
    }

    public async Task<ILlmClient> ObtenerAsync(CancellationToken ct = default)
    {
        var opt = await _accessor.ObtenerAsync(ct);
        return opt.Provider.Trim().ToLowerInvariant() switch
        {
            "openai" => new OpenAiLlmClient(_http, opt),
            "gemini" => new GeminiLlmClient(_http, opt),
            "ollama" => new OllamaLlmClient(_http, opt),
            _ => throw new InvalidOperationException(
                $"Proveedor LLM no soportado: '{opt.Provider}'. Usa openai | gemini | ollama.")
        };
    }
}
