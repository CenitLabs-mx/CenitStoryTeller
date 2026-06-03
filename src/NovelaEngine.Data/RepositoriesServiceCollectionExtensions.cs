using Microsoft.Extensions.DependencyInjection;
using NovelaEngine.Data.Repositories;
using NovelaEngine.Data.Services;

namespace NovelaEngine.Data;

public static class RepositoriesServiceCollectionExtensions
{
    public static IServiceCollection AddNovelaRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IObraRepository, ObraRepository>();
        services.AddScoped<ICapituloRepository, CapituloRepository>();
        services.AddScoped<IRegistroPasoRepository, RegistroPasoRepository>();
        services.AddSingleton<IPromptProvider, FilePromptProvider>();
        services.AddScoped<IGeneracionService, GeneracionService>();
        return services;
    }
}
