using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CenitStoryTeller.Data;

// Permite ejecutar 'dotnet ef migrations add' sin arrancar la app web.
public sealed class CenitStoryTellerDbContextFactory : IDesignTimeDbContextFactory<CenitStoryTellerDbContext>
{
    public CenitStoryTellerDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Database=cenitstoryteller;Username=postgres;Password=tuPassword";

        var options = new DbContextOptionsBuilder<CenitStoryTellerDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new CenitStoryTellerDbContext(options);
    }
}
