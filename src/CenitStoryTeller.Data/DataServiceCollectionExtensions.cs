using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CenitStoryTeller.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddNovelaData(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Falta ConnectionStrings:Default");

        services.AddDbContext<NovelaDbContext>(o => o.UseNpgsql(cs));
        return services;
    }
}
