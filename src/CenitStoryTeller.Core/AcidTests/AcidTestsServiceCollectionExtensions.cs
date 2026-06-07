using Microsoft.Extensions.DependencyInjection;

namespace CenitStoryTeller.Core.AcidTests;

public static class AcidTestsServiceCollectionExtensions
{
    public static IServiceCollection AddCenitStoryTellerAcidTests(this IServiceCollection services)
    {
        services.AddTransient<IAcidTest, FisicaTest>();
        services.AddTransient<IAcidTest, PsicologicaTest>();
        services.AddTransient<IAcidTest, AmbientalTest>();
        services.AddTransient<IAcidTest, QuimicaTest>();
        services.AddTransient<IAcidTestRunner, AcidTestRunner>();
        return services;
    }
}
