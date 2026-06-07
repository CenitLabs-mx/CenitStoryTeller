using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Data.Services;

// Resumen condensado del estado de una obra alrededor del capítulo bajo análisis:
// qué se ha escrito y aprobado antes (Pasado), qué beats quedan pendientes después
// (Futuro). Lo consume la Prueba del Ácido para detectar contradicciones contra
// capítulos previos y compromisos rotos hacia adelante.
public sealed record Compendio(string Pasado, string Futuro);

public interface ICompendioService
{
    Task<Compendio> ObtenerAsync(Guid obraId, int hastaOrden, CancellationToken ct = default);
}

public sealed class CompendioService : ICompendioService
{
    private readonly IObraRepository _obras;
    private readonly ILlmClientFactory _llmFactory;
    private readonly ILlmOptionsAccessor _opts;
    private readonly IMemoryCache _cache;

    public CompendioService(
        IObraRepository obras,
        ILlmClientFactory llmFactory,
        ILlmOptionsAccessor opts,
        IMemoryCache cache)
    {
        _obras = obras;
        _llmFactory = llmFactory;
        _opts = opts;
        _cache = cache;
    }

    public async Task<Compendio> ObtenerAsync(Guid obraId, int hastaOrden, CancellationToken ct = default)
    {
        var obra = await _obras.GetConCanonAsync(obraId, ct)
            ?? throw new InvalidOperationException($"No existe la obra {obraId}.");

        // ---- Futuro: barato y determinista, sin LLM. ----
        var beatsFuturos = obra.Beats
            .Where(b => b.Orden > hastaOrden && b.Estado != BeatEstado.Validado)
            .OrderBy(b => b.Orden)
            .ToList();

        var futuro = beatsFuturos.Count == 0
            ? "(no hay beats futuros pendientes)"
            : string.Join("\n", beatsFuturos.Select(b =>
                $"- Beat {b.Orden} ({b.Acto}/{b.Funcion}): {b.Titulo}" +
                (string.IsNullOrWhiteSpace(b.Descripcion) ? "" : $" — {b.Descripcion}")));

        // ---- Pasado: capítulos previos con su versión final aprobada. ----
        var capitulosPrevios = obra.Capitulos
            .Where(c => c.Orden < hastaOrden)
            .OrderBy(c => c.Orden)
            .ToList();

        // Solo versiones marcadas como finales: representan lo "canon" hasta ahora.
        // Si un capítulo previo aún no tiene final, lo omitimos (no es canon).
        var versionesFinales = new List<(Capitulo Cap, CapituloVersion V)>();
        foreach (var c in capitulosPrevios)
        {
            var final = c.Versiones.FirstOrDefault(v => v.EsFinal);
            if (final is not null) versionesFinales.Add((c, final));
        }

        if (versionesFinales.Count == 0)
            return new Compendio("(no hay capítulos previos aprobados)", futuro);

        // Cache hit cuando el conjunto de finales previas no cambió.
        var cacheKey = ConstruirCacheKey(obraId, hastaOrden, versionesFinales.Select(x => x.V.Id));
        if (_cache.TryGetValue<string>(cacheKey, out var pasadoCacheado) && pasadoCacheado is not null)
            return new Compendio(pasadoCacheado, futuro);

        var pasado = await ResumirPasadoAsync(versionesFinales, ct);
        _cache.Set(cacheKey, pasado, TimeSpan.FromHours(2));

        return new Compendio(pasado, futuro);
    }

    private async Task<string> ResumirPasadoAsync(
        List<(Capitulo Cap, CapituloVersion V)> versiones, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Condensa los siguientes capítulos en una crónica corta y factual " +
                      "(no embellecer, no spoilear más allá de lo escrito). Una sección por capítulo, " +
                      "máximo 6 frases por sección. Mantén nombres y lugares exactos.");
        sb.AppendLine();
        foreach (var (c, v) in versiones)
        {
            sb.AppendLine($"# Capítulo {c.Orden}: {c.Titulo}");
            sb.AppendLine(v.Texto);
            sb.AppendLine();
        }

        var opt = await _opts.ObtenerAsync(ct);
        var llm = await _llmFactory.ObtenerAsync(ct);
        var resp = await llm.CompleteAsync(new LlmRequest
        {
            Model = opt.ModelDraft,    // resumir es barato; no necesitamos el modelo fuerte.
            Temperature = 0.2,         // queremos fidelidad, no creatividad.
            Messages = new[]
            {
                LlmMessage.System("Eres un editor que condensa prosa narrativa sin inventar nada."),
                LlmMessage.User(sb.ToString())
            }
        }, ct);

        return resp.Text.Trim();
    }

    private static string ConstruirCacheKey(Guid obraId, int hastaOrden, IEnumerable<Guid> versionIds)
    {
        var concat = string.Join("|", versionIds.OrderBy(id => id).Select(id => id.ToString("N")));
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(concat)));
        return $"compendio:{obraId:N}:{hastaOrden}:{hash}";
    }
}
