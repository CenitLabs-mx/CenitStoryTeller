using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NovelaEngine.Core.Llm;

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection AddNovelaLlm(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddHttpClient();

        services.AddSingleton<ILlmClient>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return opt.Provider.Trim().ToLowerInvariant() switch
            {
                "openai" => new OpenAiLlmClient(factory, opt),
                "gemini" => new GeminiLlmClient(factory, opt),
                "ollama" => new OllamaLlmClient(factory, opt),
                _ => throw new InvalidOperationException(
                    $"Proveedor LLM no soportado: '{opt.Provider}'. Usa openai | gemini | ollama.")
            };
        });
        return services;
    }
}
