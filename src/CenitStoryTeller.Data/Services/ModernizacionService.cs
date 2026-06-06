using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Data.Services;

public interface IModernizacionService
{
    // Moderniza el canon fuente de una obra de dominio publico y lo guarda como Borrador.
    Task<Obra> ModernizarCanonAsync(Guid obraId, ParametrosModernizacion parametros, CancellationToken ct = default);
}

public sealed class ModernizacionService : IModernizacionService
{
    private readonly ILlmClient _llm;
    private readonly LlmOptions _opt;
    private readonly IPromptProvider _prompts;
    private readonly IObraRepository _obras;
    private readonly IRegistroPasoRepository _pasos;
    private readonly IUnitOfWork _uow;

    public ModernizacionService(
        ILlmClient llm,
        IOptions<LlmOptions> opt,
        IPromptProvider prompts,
        IObraRepository obras,
        IRegistroPasoRepository pasos,
        IUnitOfWork uow)
    {
        _llm = llm;
        _opt = opt.Value;
        _prompts = prompts;
        _obras = obras;
        _pasos = pasos;
        _uow = uow;
    }

    public async Task<Obra> ModernizarCanonAsync(
        Guid obraId, ParametrosModernizacion p, CancellationToken ct = default)
    {
        var obra = await _obras.GetConCanonAsync(obraId, ct)
            ?? throw new InvalidOperationException($"No existe la obra {obraId}.");

        if (obra.Intake != IntakeTipo.DominioPublico)
            throw new InvalidOperationException(
                "ModernizarCanonAsync solo aplica a obras con Intake = Dominio publico.");

        // 1) Razonamiento con el modelo fuerte (es transposicion, no prosa).
        var system = await _prompts.ModernizaAsync(ct);
        var instruccion = ModernizaParser.InstruccionJson(p, CanonSerializer.ToTexto(obra));

        var resp = await _llm.CompleteAsync(new LlmRequest
        {
            Model = _opt.ModelReview,
            Temperature = 0.4,
            Messages = new[] { LlmMessage.System(system), LlmMessage.User(instruccion) }
        }, ct);

        var dto = ModernizaParser.Parse(resp.Text);

        // 2) Personajes (Canon = Borrador).
        var personajesPorNombre = new Dictionary<string, Personaje>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.personajes ?? new())
        {
            var personaje = new Personaje
            {
                Id = Guid.NewGuid(),
                ObraId = obra.Id,
                Nombre = d.nombre,
                Rol = d.rol ?? "",
                Arquetipo = d.arquetipo ?? "",
                HeridaCentral = d.heridaCentral ?? "",
                Deseo = d.deseo ?? "",
                Necesidad = d.necesidad ?? "",
                EstadoEnTrama = d.estadoEnTrama ?? "",
                EstadoVital = ParseEnum<EstadoVital>(d.estadoVital),
                Canon = CanonNivel.Borrador
            };
            obra.Personajes.Add(personaje);
            personajesPorNombre[personaje.Nombre] = personaje;
        }

        // 3) Ubicaciones (Canon = Borrador).
        var ubicacionesPorNombre = new Dictionary<string, Ubicacion>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dto.ubicaciones ?? new())
        {
            var ubicacion = new Ubicacion
            {
                Id = Guid.NewGuid(),
                ObraId = obra.Id,
                Nombre = d.nombre,
                Tipo = d.tipo ?? "",
                RolEnTrama = d.rolEnTrama ?? "",
                EstadoActual = d.estadoActual ?? "",
                Canon = CanonNivel.Borrador
            };
            obra.Ubicaciones.Add(ubicacion);
            ubicacionesPorNombre[ubicacion.Nombre] = ubicacion;
        }

        // 4) Beats.
        foreach (var d in dto.beats ?? new())
        {
            obra.Beats.Add(new Beat
            {
                Id = Guid.NewGuid(),
                ObraId = obra.Id,
                Titulo = d.titulo,
                Orden = d.orden,
                Acto = ParseEnum<Acto>(d.acto),
                Funcion = ParseEnum<FuncionNarrativa>(d.funcionNarrativa),
                Descripcion = d.descripcion ?? "",
                Estado = BeatEstado.Pendiente
            });
        }

        // 5) Eventos (resuelve las relaciones N:N por nombre contra lo recien creado).
        foreach (var d in dto.eventos ?? new())
        {
            var evento = new Evento
            {
                Id = Guid.NewGuid(),
                ObraId = obra.Id,
                Titulo = d.titulo,
                Orden = d.orden,
                Acto = ParseEnum<Acto>(d.acto),
                Tipo = ParseEnum<EventoTipo>(d.tipo),
                MomentoInWorld = d.momento ?? "",
                CambioConsecuencia = d.cambioEstado ?? ""
            };
            foreach (var n in d.personajes ?? new())
                if (personajesPorNombre.TryGetValue(n, out var per)) evento.Personajes.Add(per);
            foreach (var n in d.ubicaciones ?? new())
                if (ubicacionesPorNombre.TryGetValue(n, out var ub)) evento.Ubicaciones.Add(ub);
            obra.Eventos.Add(evento);
        }

        // 6) Rastro auditable (guarda la salida integra del modelo en CanonSnapshot).
        await _pasos.AppendAsync(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Agente = "Moderniza",
            Accion = $"Modernizacion del canon a {p.EpocaDestino} ({p.Registro})",
            CanonSnapshot = resp.Text,
            Cambios = $"+{(dto.personajes?.Count ?? 0)} personajes, " +
                      $"+{(dto.ubicaciones?.Count ?? 0)} ubicaciones, " +
                      $"+{(dto.beats?.Count ?? 0)} beats, " +
                      $"+{(dto.eventos?.Count ?? 0)} eventos (Canon=Borrador).",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return obra;
    }

    private static TEnum ParseEnum<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var r) ? r : default;
}
