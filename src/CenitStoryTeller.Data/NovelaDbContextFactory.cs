using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CenitStoryTeller.Data;

// Permite ejecutar 'dotnet ef migrations add' sin arrancar la app web.
public sealed class NovelaDbContextFactory : IDesignTimeDbContextFactory<NovelaDbContext>
{
    public NovelaDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Database=novela;Username=postgres;Password=tuPassword";

        var options = new DbContextOptionsBuilder<NovelaDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new NovelaDbContext(options);
    }
}
