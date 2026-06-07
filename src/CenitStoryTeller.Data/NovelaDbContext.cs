using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Data.Entities;

namespace CenitStoryTeller.Data;

public sealed class NovelaDbContext : IdentityDbContext<Usuario, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public NovelaDbContext(DbContextOptions<NovelaDbContext> options) : base(options) { }

    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Personaje> Personajes => Set<Personaje>();
    public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
    public DbSet<Beat> Beats => Set<Beat>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Capitulo> Capitulos => Set<Capitulo>();
    public DbSet<CapituloVersion> CapituloVersiones => Set<CapituloVersion>();
    public DbSet<PruebaAcido> PruebasAcido => Set<PruebaAcido>();
    public DbSet<RegistroPaso> RegistrosPaso => Set<RegistroPaso>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        // Todos los enums se guardan como texto legible en la BD (no como int).
        cb.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // IdentityDbContext configura las tablas AspNetUsers/Roles/etc.
        base.OnModelCreating(b);

        b.Entity<Usuario>(e =>
        {
            e.Property(u => u.NombreDisplay).HasMaxLength(200);
        });

        // ---- Obra: raíz del agregado ----
        b.Entity<Obra>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            e.HasQueryFilter(x => x.EliminadaEn == null);
            e.HasMany(x => x.Personajes).WithOne().HasForeignKey(p => p.ObraId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Ubicaciones).WithOne().HasForeignKey(u => u.ObraId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Beats).WithOne().HasForeignKey(bt => bt.ObraId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Eventos).WithOne().HasForeignKey(ev => ev.ObraId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Capitulos).WithOne().HasForeignKey(c => c.ObraId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Personaje>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ObraId, x.Nombre });
        });

        b.Entity<Ubicacion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ObraId, x.Nombre });
        });

        b.Entity<Beat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.ObraId, x.Orden });
        });

        b.Entity<Evento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.ObraId, x.Orden });

            // N:N Evento <-> Personaje (tabla puente EventoPersonaje)
            e.HasMany(x => x.Personajes).WithMany()
                .UsingEntity(j => j.ToTable("EventoPersonaje"));

            // N:N Evento <-> Ubicacion (tabla puente EventoUbicacion)
            e.HasMany(x => x.Ubicaciones).WithMany()
                .UsingEntity(j => j.ToTable("EventoUbicacion"));
        });

        b.Entity<Capitulo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.ObraId, x.Orden });
            e.HasMany(x => x.Versiones).WithOne(v => v.Capitulo).HasForeignKey(v => v.CapituloId).OnDelete(DeleteBehavior.Cascade);

            // Beat objetivo opcional: si se borra el beat, el capítulo queda sin referencia (no se borra).
            e.HasOne<Beat>().WithMany().HasForeignKey(x => x.BeatObjetivoId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<CapituloVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Modelo).HasMaxLength(120);
            // No puede haber dos versiones con el mismo numero en un capitulo.
            e.HasIndex(x => new { x.CapituloId, x.NumeroVersion }).IsUnique();

            // 1:1 con su Prueba del Ácido.
            e.HasOne(x => x.Acido).WithOne()
                .HasForeignKey<PruebaAcido>(p => p.CapituloVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PruebaAcido>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CapituloVersionId).IsUnique();
        });

        b.Entity<RegistroPaso>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Agente).HasMaxLength(80);
            e.HasIndex(x => new { x.ObraId, x.Timestamp });
            e.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
