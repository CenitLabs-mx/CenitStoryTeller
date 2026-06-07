using Microsoft.Extensions.DependencyInjection;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Repositories;
using CenitStoryTeller.Data.Services;

namespace CenitStoryTeller.Data;

public static class RepositoriesServiceCollectionExtensions
{
    public static IServiceCollection AddCenitStoryTellerRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IObraRepository, ObraRepository>();
        services.AddScoped<ICapituloRepository, CapituloRepository>();
        services.AddScoped<IRegistroPasoRepository, RegistroPasoRepository>();
        services.AddScoped<IConfiguracionLlmRepository, ConfiguracionLlmRepository>();
        // Sobrescribe el accessor por defecto (que mira solo appsettings) con el que
        // resuelve por usuario. Como AddCenitStoryTellerLlm corre antes, la última registración gana.
        services.AddScoped<ILlmOptionsAccessor, UserLlmOptionsAccessor>();
        services.AddSingleton<IPromptProvider, FilePromptProvider>();
        services.AddScoped<IGeneracionService, GeneracionService>();
        services.AddScoped<IModernizacionService, ModernizacionService>();
        services.AddScoped<ICompendioService, CompendioService>();
        services.AddScoped<IBeatService, BeatService>();
        return services;
    }
}
