using System;
using System.Text.Json;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Data.Services;

// Mapea el JSON del revisor a una PruebaAcido. Tolera texto alrededor del bloque JSON.
internal static class AcidoParser
{
    private sealed record Dto(
        bool fisica, bool psicologica, bool ambiental, bool quimica,
        bool? anacronismo, bool? fidelidad_funcional,
        string? veredicto, string? hallazgos, string? parches);

    public static PruebaAcido Parse(string respuesta)
    {
        var inicio = respuesta.IndexOf('{');
        var fin = respuesta.LastIndexOf('}');
        if (inicio < 0 || fin <= inicio)
            throw new FormatException("La respuesta del revisor no contiene un JSON válido.");

        var dto = JsonSerializer.Deserialize<Dto>(
            respuesta[inicio..(fin + 1)],
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new FormatException("No se pudo parsear la prueba del ácido.");

        return new PruebaAcido
        {
            Fisica = dto.fisica,
            Psicologica = dto.psicologica,
            Ambiental = dto.ambiental,
            Quimica = dto.quimica,
            Anacronismo = dto.anacronismo ?? false,
            FidelidadFuncional = dto.fidelidad_funcional ?? true,
            Veredicto = Enum.TryParse<Veredicto>(dto.veredicto, ignoreCase: true, out var v)
                ? v : Veredicto.Revisar,
            Hallazgos = dto.hallazgos ?? "",
            Parches = dto.parches ?? ""
        };
    }
}
