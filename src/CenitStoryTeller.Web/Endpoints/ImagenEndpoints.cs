using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Data;

namespace CenitStoryTeller.Web.Endpoints;

// Endpoints HTTP para imágenes de personajes y ubicaciones. Minimal API porque
// son operaciones binarias (upload/serve) que se manejan mejor fuera de Blazor.
public static class ImagenEndpoints
{
    // 2 MB. Más que suficiente para un avatar; arriba de eso pedimos resize al usuario.
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public static void MapImagenEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api").RequireAuthorization();

        grupo.MapGet("/personajes/{id:guid}/imagen", ServirImagenPersonaje)
             .AllowAnonymous();   // visible para todos: simplifica el render en cards
        grupo.MapPost("/personajes/{id:guid}/imagen", SubirImagenPersonaje)
             .DisableAntiforgery()    // POST API, autorizado por cookie
             .WithName("SubirImagenPersonaje");
        grupo.MapDelete("/personajes/{id:guid}/imagen", BorrarImagenPersonaje);

        grupo.MapGet("/ubicaciones/{id:guid}/imagen", ServirImagenUbicacion).AllowAnonymous();
        grupo.MapPost("/ubicaciones/{id:guid}/imagen", SubirImagenUbicacion).DisableAntiforgery();
        grupo.MapDelete("/ubicaciones/{id:guid}/imagen", BorrarImagenUbicacion);
    }

    // ---- Personaje ----

    private static async Task<IResult> ServirImagenPersonaje(
        Guid id, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var p = await db.Personajes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p?.ImagenBytes is null) return Results.NotFound();
        return Results.File(p.ImagenBytes, p.ImagenContentType ?? "application/octet-stream");
    }

    private static async Task<IResult> SubirImagenPersonaje(
        Guid id, IFormFile archivo, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var err = ValidarArchivo(archivo);
        if (err is not null) return Results.BadRequest(err);

        var p = await db.Personajes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        p.ImagenBytes = ms.ToArray();
        p.ImagenContentType = archivo.ContentType;
        await db.SaveChangesAsync(ct);

        return Results.Ok();
    }

    private static async Task<IResult> BorrarImagenPersonaje(
        Guid id, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var p = await db.Personajes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();
        p.ImagenBytes = null;
        p.ImagenContentType = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ---- Ubicacion ----

    private static async Task<IResult> ServirImagenUbicacion(
        Guid id, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var u = await db.Ubicaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u?.ImagenBytes is null) return Results.NotFound();
        return Results.File(u.ImagenBytes, u.ImagenContentType ?? "application/octet-stream");
    }

    private static async Task<IResult> SubirImagenUbicacion(
        Guid id, IFormFile archivo, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var err = ValidarArchivo(archivo);
        if (err is not null) return Results.BadRequest(err);

        var u = await db.Ubicaciones.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return Results.NotFound();

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        u.ImagenBytes = ms.ToArray();
        u.ImagenContentType = archivo.ContentType;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> BorrarImagenUbicacion(
        Guid id, CenitStoryTellerDbContext db, CancellationToken ct)
    {
        var u = await db.Ubicaciones.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return Results.NotFound();
        u.ImagenBytes = null;
        u.ImagenContentType = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static string? ValidarArchivo(IFormFile archivo)
    {
        if (archivo.Length == 0) return "Archivo vacío.";
        if (archivo.Length > MaxBytes) return $"Imagen demasiado grande (máx {MaxBytes / 1024 / 1024} MB).";
        if (!TiposPermitidos.Contains(archivo.ContentType))
            return $"Tipo no soportado: {archivo.ContentType}. Usa JPEG, PNG, WebP o GIF.";
        return null;
    }
}
