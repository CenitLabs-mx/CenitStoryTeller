using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NovelaEngine.Core.Llm;
using NovelaEngine.Data.Entities;
using NovelaEngine.Data.Repositories;

namespace NovelaEngine.Data.Services;

public record ParametrosModernizacion(
    string EpocaDestino,
    string LugarCultura,
    string Registro,
    string PlataformaObjetivo,
    string NivelFidelidad,
    string Tono);

public interface IGeneracionService
{
    // Escribe un borrador del capítulo con ModelDraft y lo guarda como nueva versión.
    Task<CapituloVersion> GenerarBorradorAsync(Guid capituloId, string promptUsuario, CancellationToken ct = default);

    // Corre la prueba del ácido sobre una versión con ModelReview y guarda el veredicto.
    Task<PruebaAcido> CorrerPruebaAcidoAsync(Guid capituloVersionId, CancellationToken ct = default);

    // Moderniza el canon completo de la obra a partir de una obra clásica.
    Task ModernizarCanonAsync(Guid obraId, ParametrosModernizacion parametros, CancellationToken ct = default);
}

public sealed class GeneracionService : IGeneracionService
{
    private readonly ILlmClient _llm;
    private readonly LlmOptions _opt;
    private readonly IPromptProvider _prompts;
    private readonly ICapituloRepository _capitulos;
    private readonly IObraRepository _obras;
    private readonly IRegistroPasoRepository _pasos;
    private readonly IUnitOfWork _uow;

    public GeneracionService(
        ILlmClient llm,
        IOptions<LlmOptions> opt,
        IPromptProvider prompts,
        ICapituloRepository capitulos,
        IObraRepository obras,
        IRegistroPasoRepository pasos,
        IUnitOfWork uow)
    {
        _llm = llm;
        _opt = opt.Value;
        _prompts = prompts;
        _capitulos = capitulos;
        _obras = obras;
        _pasos = pasos;
        _uow = uow;
    }

    public async Task<CapituloVersion> GenerarBorradorAsync(
        Guid capituloId, string promptUsuario, CancellationToken ct = default)
    {
        var capitulo = await _capitulos.GetAsync(capituloId, ct)
            ?? throw new InvalidOperationException($"No existe el capítulo {capituloId}.");

        var system = await _prompts.MotorDeHistoriaAsync(ct);
        var resp = await _llm.CompleteAsync(new LlmRequest
        {
            Model = _opt.ModelDraft,          // borrador con modelo barato
            Temperature = 0.9,
            Messages = new[]
            {
                LlmMessage.System(system),
                LlmMessage.User(promptUsuario)
            }
        }, ct);

        var numero = await _capitulos.SiguienteNumeroVersionAsync(capituloId, ct);
        var version = new CapituloVersion
        {
            Id = Guid.NewGuid(),
            CapituloId = capituloId,
            NumeroVersion = numero,
            Modelo = resp.Model,
            PromptUsado = promptUsuario,
            Texto = resp.Text,
            EsFinal = false,
            CreadoEn = DateTimeOffset.UtcNow
        };
        await _capitulos.AgregarVersionAsync(version, ct);

        await _pasos.AppendAsync(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = capitulo.ObraId,
            Agente = "Motor de Historia",
            Accion = $"Borrador v{numero} del capítulo {capituloId}",
            Cambios = $"{resp.Text.Length} caracteres generados con {resp.Model}.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return version;
    }

    public async Task<PruebaAcido> CorrerPruebaAcidoAsync(
        Guid capituloVersionId, CancellationToken ct = default)
    {
        var version = await _capitulos.GetVersionAsync(capituloVersionId, ct)
            ?? throw new InvalidOperationException($"No existe la versión {capituloVersionId}.");

        var system = await _prompts.ContinuidadAcidoAsync(ct);
        var resp = await _llm.CompleteAsync(new LlmRequest
        {
            Model = _opt.ModelReview,         // validación con modelo fuerte
            Temperature = 0.2,
            Messages = new[]
            {
                LlmMessage.System(system),
                LlmMessage.User(version.Texto)
            }
        }, ct);

        var prueba = AcidoParser.Parse(resp.Text);
        prueba.Id = Guid.NewGuid();
        prueba.CapituloVersionId = version.Id;
        await _capitulos.GuardarPruebaAcidoAsync(prueba, ct);

        await _pasos.AppendAsync(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = version.Capitulo!.ObraId,
            Agente = "Continuidad y Prueba del Ácido",
            Accion = $"Prueba del ácido sobre versión {version.Id}",
            Cambios = $"Veredicto: {prueba.Veredicto}.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return prueba;
    }

    public async Task ModernizarCanonAsync(Guid obraId, ParametrosModernizacion p, CancellationToken ct = default)
    {
        var obra = await _obras.GetConCanonAsync(obraId, ct)
            ?? throw new InvalidOperationException($"No existe la obra {obraId}.");

        var systemPrompt = await _prompts.ModernizarAsync(ct);

        var sourceCanonBuilder = new System.Text.StringBuilder();
        sourceCanonBuilder.AppendLine("PERSONAJES:");
        foreach (var pj in obra.Personajes)
        {
            sourceCanonBuilder.AppendLine($"- Nombre: {pj.Nombre}, Rol: {pj.Rol}, Arquetipo: {pj.Arquetipo}, Herida Central: {pj.HeridaCentral}, Deseo: {pj.Deseo}, Necesidad: {pj.Necesidad}, Restricciones: {pj.Restricciones}, Estado en Trama: {pj.EstadoEnTrama}, Estado Vital: {pj.EstadoVital}");
        }
        sourceCanonBuilder.AppendLine("\nUBICACIONES:");
        foreach (var ub in obra.Ubicaciones)
        {
            sourceCanonBuilder.AppendLine($"- Nombre: {ub.Nombre}, Tipo: {ub.Tipo}, Rol en Trama: {ub.RolEnTrama}, Estado Actual: {ub.EstadoActual}");
        }
        sourceCanonBuilder.AppendLine("\nBEATS:");
        foreach (var bt in obra.Beats)
        {
            sourceCanonBuilder.AppendLine($"- Orden: {bt.Orden}, Título: {bt.Titulo}, Acto: {bt.Acto}, Función: {bt.Funcion}, Descripción: {bt.Descripcion}");
        }
        sourceCanonBuilder.AppendLine("\nEVENTOS:");
        foreach (var ev in obra.Eventos)
        {
            sourceCanonBuilder.AppendLine($"- Orden: {ev.Orden}, Título: {ev.Titulo}, Acto: {ev.Acto}, Tipo: {ev.Tipo}, Momento In-World: {ev.MomentoInWorld}, Cambio/Consecuencia: {ev.CambioConsecuencia}");
        }

        var userMessage = $@"EPOCA DESTINO: {p.EpocaDestino}
LUGAR/CULTURA: {p.LugarCultura}
REGISTRO: {p.Registro}
PLATAFORMA OBJETIVO: {p.PlataformaObjetivo}
NIVEL FIDELIDAD: {p.NivelFidelidad}
TONO: {p.Tono}

CANON FUENTE:
{sourceCanonBuilder.ToString()}";

        var resp = await _llm.CompleteAsync(new LlmRequest
        {
            Model = _opt.ModelReview,
            Temperature = 0.2,
            Messages = new[]
            {
                LlmMessage.System(systemPrompt),
                LlmMessage.User(userMessage)
            }
        }, ct);

        var responseText = resp.Text;
        var jsonStart = responseText.IndexOf('{');
        var jsonEnd = responseText.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonContent = responseText[jsonStart..(jsonEnd + 1)];
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("personajes", out var personajesProp) && personajesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in personajesProp.EnumerateArray())
                 {
                    var pj = new Personaje
                    {
                        Id = Guid.NewGuid(),
                        ObraId = obraId,
                        Canon = CanonNivel.Borrador,
                        Nombre = item.GetProperty("nombre").GetString() ?? "",
                        Rol = item.TryGetProperty("rol", out var r) ? r.GetString() : null,
                        Arquetipo = item.TryGetProperty("arquetipo", out var aq) ? aq.GetString() : null,
                        HeridaCentral = item.TryGetProperty("heridaCentral", out var hc) ? hc.GetString() : null,
                        Deseo = item.TryGetProperty("deseo", out var d) ? d.GetString() : null,
                        Necesidad = item.TryGetProperty("necesidad", out var n) ? n.GetString() : null,
                        Restricciones = item.TryGetProperty("restricciones", out var re) ? re.GetString() : null,
                        EstadoEnTrama = item.TryGetProperty("estadoEnTrama", out var et) ? et.GetString() : null,
                        EstadoVital = EstadoVital.Vivo
                    };
                    obra.Personajes.Add(pj);
                }
            }

            if (root.TryGetProperty("ubicaciones", out var ubicacionesProp) && ubicacionesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in ubicacionesProp.EnumerateArray())
                {
                    var ub = new Ubicacion
                    {
                        Id = Guid.NewGuid(),
                        ObraId = obraId,
                        Canon = CanonNivel.Borrador,
                        Nombre = item.GetProperty("nombre").GetString() ?? "",
                        Tipo = item.TryGetProperty("tipo", out var t) ? t.GetString() : null,
                        RolEnTrama = item.TryGetProperty("rolEnTrama", out var rt) ? rt.GetString() : null,
                        EstadoActual = item.TryGetProperty("estadoActual", out var ea) ? ea.GetString() : null
                    };
                    obra.Ubicaciones.Add(ub);
                }
            }

            if (root.TryGetProperty("beats", out var beatsProp) && beatsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in beatsProp.EnumerateArray())
                {
                    var bt = new Beat
                    {
                        Id = Guid.NewGuid(),
                        ObraId = obraId,
                        Estado = BeatEstado.Pendiente,
                        Titulo = item.GetProperty("titulo").GetString() ?? "",
                        Orden = item.GetProperty("orden").GetInt32(),
                        Acto = item.TryGetProperty("acto", out var act) && Enum.TryParse<Acto>(act.GetString(), true, out var aVal) ? aVal : Acto.Setup,
                        Funcion = item.TryGetProperty("funcion", out var func) && Enum.TryParse<FuncionNarrativa>(func.GetString(), true, out var fVal) ? fVal : FuncionNarrativa.Setup,
                        Descripcion = item.TryGetProperty("descripcion", out var desc) ? desc.GetString() : null
                    };
                    obra.Beats.Add(bt);
                }
            }

            if (root.TryGetProperty("eventos", out var eventosProp) && eventosProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in eventosProp.EnumerateArray())
                {
                    var ev = new Evento
                    {
                        Id = Guid.NewGuid(),
                        ObraId = obraId,
                        Tipo = EventoTipo.Borrador,
                        Titulo = item.GetProperty("titulo").GetString() ?? "",
                        Orden = item.GetProperty("orden").GetInt32(),
                        Acto = item.TryGetProperty("acto", out var act) && Enum.TryParse<Acto>(act.GetString(), true, out var aVal) ? aVal : Acto.Setup,
                        MomentoInWorld = item.TryGetProperty("momentoInWorld", out var miw) ? miw.GetString() : null,
                        CambioConsecuencia = item.TryGetProperty("cambioConsecuencia", out var cc) ? cc.GetString() : null
                    };
                    obra.Eventos.Add(ev);
                }
            }
        }
        else
        {
            throw new FormatException("La respuesta del modernizador no contiene un JSON válido.");
        }

        await _pasos.AppendAsync(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = obraId,
            Agente = "Moderniza",
            Accion = "Modernización completa del canon",
            Cambios = $"Canon modernizado generado con {resp.Model} para la época {p.EpocaDestino}.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
    }
}
