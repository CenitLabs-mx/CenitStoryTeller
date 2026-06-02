using Microsoft.EntityFrameworkCore;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Data;

public class NovelaDbContext : DbContext
{
    public DbSet<Obra> Obras { get; set; } = null!;
    public DbSet<Personaje> Personajes { get; set; } = null!;
    public DbSet<Ubicacion> Ubicaciones { get; set; } = null!;
    public DbSet<Beat> Beats { get; set; } = null!;
    public DbSet<Evento> Eventos { get; set; } = null!;
    public DbSet<Capitulo> Capitulos { get; set; } = null!;
    public DbSet<CapituloVersion> CapituloVersiones { get; set; } = null!;
    public DbSet<PruebaAcido> PruebasAcidas { get; set; } = null!;
    public DbSet<RegistroPaso> RegistrosPasos { get; set; } = null!;

    public NovelaDbContext(DbContextOptions<NovelaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Many-to-Many relationships explicitly since Personaje and Ubicacion do not have collection navigation properties back to Evento
        modelBuilder.Entity<Evento>()
            .HasMany(e => e.Personajes)
            .WithMany();

        modelBuilder.Entity<Evento>()
            .HasMany(e => e.Ubicaciones)
            .WithMany();
            
        // Configure relationships
        modelBuilder.Entity<Obra>()
            .HasMany(o => o.Personajes)
            .WithOne()
            .HasForeignKey(p => p.ObraId);

        modelBuilder.Entity<Obra>()
            .HasMany(o => o.Ubicaciones)
            .WithOne()
            .HasForeignKey(u => u.ObraId);

        modelBuilder.Entity<Obra>()
            .HasMany(o => o.Beats)
            .WithOne()
            .HasForeignKey(b => b.ObraId);

        modelBuilder.Entity<Obra>()
            .HasMany(o => o.Eventos)
            .WithOne()
            .HasForeignKey(e => e.ObraId);

        modelBuilder.Entity<Obra>()
            .HasMany(o => o.Capitulos)
            .WithOne()
            .HasForeignKey(c => c.ObraId);

        modelBuilder.Entity<Capitulo>()
            .HasMany(c => c.Versiones)
            .WithOne()
            .HasForeignKey(v => v.CapituloId);

        modelBuilder.Entity<CapituloVersion>()
            .HasOne(v => v.Acido)
            .WithOne()
            .HasForeignKey<PruebaAcido>(a => a.CapituloVersionId);
    }
}
