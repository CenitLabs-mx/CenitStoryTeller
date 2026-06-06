using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Core.AcidTests;

/// Base común: arma el prompt, llama al LLM con baja temperatura y parsea un
/// JSON pequeño y estable. Cada dimensión solo define su criterio y su contexto.
public abstract class AcidTestBase : IAcidTest
{
    public abstract AcidDimension Dimension { get; }

    // Etiqueta legible para el prompt y los logs (acentos incluidos).
    protected abstract string EtiquetaPrompt { get; }

    // Qué debe verificar exactamente esta dimensión.
    protected abstract string Criterio { get; }

    // Fragmento de canon relevante para esta dimensión.
    protected abstract string ContextoRelevante(AcidContext ctx);

    public async Task<AcidResult> EvaluarAsync(AcidContext ctx, ILlmClient llm, CancellationToken ct)
    {
        var resp = await llm.CompleteAsync(new LlmRequest
        {
            Temperature = 0.2, // validar, no inventar
            Messages = new[]
            {
                LlmMessage.System(ConstruirSystem()),
                LlmMessage.User(ConstruirUser(ctx))
            }
        }, ct);

        return Parse(resp.Text);
    }

    private string ConstruirSystem()
    {
        var esquema =
            "{\"pasa\": true|false, " +
            "\"hallazgo\": \"si falla, explica el problema citando el canon violado; si pasa, null\", " +
            "\"parche\": \"propuesta mínima y concreta para corregir; si pasa, null\"}";

        return $"Eres un validador de coherencia narrativa especializado en la dimensión {EtiquetaPrompt} " +
               "de la Prueba del Ácido. No reescribes prosa: validas. " +
               Criterio + " " +
               "Responde ÚNICAMENTE con un bloque JSON, sin texto adicional, con esta forma exacta: " +
               esquema + ".";
    }

    protected virtual string ConstruirUser(AcidContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Capítulo: {ctx.Capitulo.Titulo} (orden {ctx.Capitulo.Orden})");
        sb.AppendLine();
        sb.AppendLine("## Contexto del canon");
        sb.AppendLine(ContextoRelevante(ctx));
        sb.AppendLine();
        sb.AppendLine("## Prosa propuesta a evaluar");
        sb.AppendLine(ctx.ProsaPropuesta);
        return sb.ToString();
    }

    // Parseo tolerante: extrae el primer objeto JSON; ante fallo, no bloquea el pipeline.
    protected static AcidResult Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AcidResult(true, null, null);

        var inicio = raw.IndexOf('{');
        var fin = raw.LastIndexOf('}');
        if (inicio < 0 || fin <= inicio)
            return new AcidResult(true, null, null);

        try
        {
            using var doc = JsonDocument.Parse(raw[inicio..(fin + 1)]);
            var root = doc.RootElement;

            var pasa = LeerPasa(root);
            var hallazgo = LeerTexto(root, "hallazgo");
            var parche = LeerTexto(root, "parche");

            return new AcidResult(pasa, hallazgo, parche);
        }
        catch (JsonException)
        {
            return new AcidResult(true, "No se pudo parsear la respuesta del validador.", null);
        }
    }

    private static bool LeerPasa(JsonElement root)
    {
        if (!root.TryGetProperty("pasa", out var p)) return true; // por defecto: pasa
        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => !bool.TryParse(p.GetString(), out var b) || b,
            _ => true
        };
    }

    private static string? LeerTexto(JsonElement root, string prop)
    {
        if (root.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
        {
            var s = v.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s!.Trim();
        }
        return null;
    }

    // ---- Helpers de formato reutilizables por las dimensiones ----
    protected static string Coalesce(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!.Trim();

    protected static string UbicacionBreve(AcidContext ctx) =>
        ctx.Ubicacion is null
            ? "(sin ubicación registrada)"
            : $"{ctx.Ubicacion.Nombre} ({Coalesce(ctx.Ubicacion.Tipo)}). " +
              $"Rol: {Coalesce(ctx.Ubicacion.RolEnTrama)}. Estado actual: {Coalesce(ctx.Ubicacion.EstadoActual)}.";

    protected static string EventosBreve(AcidContext ctx) =>
        ctx.EventosPrevios.Count == 0
            ? "(sin eventos previos registrados)"
            : string.Join("\n", ctx.EventosPrevios
                .OrderBy(e => e.Orden)
                .Select(e => $"- [{e.Orden}] {e.Titulo}: {Coalesce(e.CambioConsecuencia)}"));
}
