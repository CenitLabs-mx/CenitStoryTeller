using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using NovelaEngine.Data.Entities;

namespace NovelaEngine.Data.Services;

public enum NivelFidelidad { Fiel, Libre }

public sealed record ParametrosModernizacion
{
    public required string EpocaDestino { get; init; }       // "2020s"
    public required string LugarCultura { get; init; }       // "Mexico urbano"
    public required string Registro { get; init; }           // "juvenil/Wattpad"
    public string? PlataformaObjetivo { get; init; }         // "Wattpad"
    public NivelFidelidad Fidelidad { get; init; } = NivelFidelidad.Libre;
    public string? Tono { get; init; }                       // "dramatico-emocional"
}

// Serializa el canon fuente a texto plano para alimentar el prompt.
internal static class CanonSerializer
{
    public static string ToTexto(Obra obra)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"OBRA: {obra.Titulo}");

        sb.AppendLine("PERSONAJES:");
        foreach (var p in obra.Personajes)
            sb.AppendLine($"- {p.Nombre} | rol: {p.Rol} | herida: {p.HeridaCentral} | deseo: {p.Deseo}");

        sb.AppendLine("UBICACIONES:");
        foreach (var u in obra.Ubicaciones)
            sb.AppendLine($"- {u.Nombre} | tipo: {u.Tipo} | rol: {u.RolEnTrama}");

        sb.AppendLine("BEATS:");
        foreach (var b in obra.Beats.OrderBy(x => x.Orden))
            sb.AppendLine($"- [{b.Orden}] {b.Titulo} | funcion: {b.Funcion} | {b.Descripcion}");

        sb.AppendLine("EVENTOS:");
        foreach (var e in obra.Eventos.OrderBy(x => x.Orden))
            sb.AppendLine($"- [{e.Orden}] {e.Titulo} | tipo: {e.Tipo} | {e.CambioConsecuencia}");

        return sb.ToString();
    }
}

// Construye la instruccion y parsea el canon modernizado (JSON).
internal static class ModernizaParser
{
    public sealed record TransposicionDto(string original, string moderno);
    public sealed record PersonajeDto(string nombre, string? rol, string? arquetipo,
        string? heridaCentral, string? deseo, string? necesidad, string? estadoEnTrama, string? estadoVital);
    public sealed record UbicacionDto(string nombre, string? tipo, string? rolEnTrama, string? estadoActual);
    public sealed record BeatDto(string titulo, int orden, string? acto, string? funcionNarrativa, string? descripcion);
    public sealed record EventoDto(string titulo, int orden, string? acto, string? tipo,
        string? momento, string? cambioEstado, List<string>? personajes, List<string>? ubicaciones);

    public sealed record CanonModernizadoDto(
        List<TransposicionDto>? transposicion,
        List<TransposicionDto>? motivadores,
        List<PersonajeDto>? personajes,
        List<UbicacionDto>? ubicaciones,
        List<BeatDto>? beats,
        List<EventoDto>? eventos,
        List<string>? supuestos,
        List<string>? riesgos);

    public static string InstruccionJson(ParametrosModernizacion p, string canonFuente)
    {
        var encabezado = $"""
            PARAMETROS DE MODERNIZACION
            - Epoca destino: {p.EpocaDestino}
            - Lugar/cultura: {p.LugarCultura}
            - Registro: {p.Registro}
            - Plataforma: {p.PlataformaObjetivo ?? "(sin especificar)"}
            - Fidelidad: {p.Fidelidad}
            - Tono: {p.Tono ?? "(libre)"}

            CANON FUENTE
            {canonFuente}
            """;

        return encabezado + Environment.NewLine + EsquemaJson;
    }

    // Esquema sin interpolacion: las llaves del JSON son literales.
    private const string EsquemaJson = """
        Devuelve UNICAMENTE un objeto JSON (sin texto extra) con esta forma:
        {
          "transposicion": [{ "original": "", "moderno": "" }],
          "motivadores":   [{ "original": "", "moderno": "" }],
          "personajes":    [{ "nombre": "", "rol": "", "arquetipo": "", "heridaCentral": "", "deseo": "", "necesidad": "", "estadoEnTrama": "", "estadoVital": "Vivo" }],
          "ubicaciones":   [{ "nombre": "", "tipo": "", "rolEnTrama": "", "estadoActual": "" }],
          "beats":         [{ "titulo": "", "orden": 1, "acto": "", "funcionNarrativa": "", "descripcion": "" }],
          "eventos":       [{ "titulo": "", "orden": 1, "acto": "", "tipo": "", "momento": "", "cambioEstado": "", "personajes": [], "ubicaciones": [] }],
          "supuestos": [],
          "riesgos": []
        }
        """;

    public static CanonModernizadoDto Parse(string respuesta)
    {
        var inicio = respuesta.IndexOf('{');
        var fin = respuesta.LastIndexOf('}');
        if (inicio < 0 || fin <= inicio)
            throw new FormatException("La respuesta de MODERNIZA no contiene un JSON valido.");

        return JsonSerializer.Deserialize<CanonModernizadoDto>(
            respuesta[inicio..(fin + 1)],
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new FormatException("No se pudo parsear el canon modernizado.");
    }
}
