using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Core.Entities;
using CenitStoryTeller.Data.Repositories;

namespace CenitStoryTeller.Data.Services;

public interface IGeneracionService
{
    // Escribe un borrador del capítulo con ModelDraft y lo guarda como nueva versión.
    Task<CapituloVersion> GenerarBorradorAsync(Guid capituloId, string promptUsuario, CancellationToken ct = default);

    // Corre la prueba del ácido sobre una versión con ModelReview y guarda el veredicto.
    Task<PruebaAcido> CorrerPruebaAcidoAsync(Guid capituloVersionId, CancellationToken ct = default);

    // Moderniza el canon fuente de una obra de dominio publico y lo guarda como Borrador.
    Task<Obra> ModernizarCanonAsync(Guid obraId, ParametrosModernizacion parametros, CancellationToken ct = default);
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
    private readonly IAcidTestRunner _acidTests;   // NUEVO

    public GeneracionService(
        ILlmClient llm,
        IOptions<LlmOptions> opt,
        IPromptProvider prompts,
        ICapituloRepository capitulos,
        IObraRepository obras,
        IRegistroPasoRepository pasos,
        IUnitOfWork uow,
        IAcidTestRunner acidTests)   // NUEVO
    {
        _llm = llm;
        _opt = opt.Value;
        _prompts = prompts;
        _capitulos = capitulos;
        _obras = obras;
        _pasos = pasos;
        _uow = uow;
        _acidTests = acidTests;   // NUEVO
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

        // 1) Chequeo holístico: un solo prompt con el modelo fuerte da el veredicto global.
        var system = await _prompts.ContinuidadAcidoAsync(ct);
        var resp = await _llm.CompleteAsync(new LlmRequest
        {
            Model = _opt.ModelReview,
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

        // 2) Si el veredicto pide revisión, localizamos dimensión por dimensión con los
        //    4 AcidTests basados en LLM y enriquecemos hallazgos/parches.
        var localizadas = 0;
        if (prueba.Veredicto != Veredicto.Aprobado)
        {
            var ctx = await ConstruirAcidContextAsync(version, ct);
            if (ctx is not null)
            {
                var resultados = await _acidTests.EjecutarAsync(ctx, ct);
                FusionarDimensiones(prueba, resultados);
                localizadas = resultados.Count;
            }
        }

        await _capitulos.GuardarPruebaAcidoAsync(prueba, ct);

        await _pasos.AppendAsync(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = version.Capitulo!.ObraId,
            Agente = "Continuidad y Prueba del Ácido",
            Accion = $"Prueba del ácido sobre versión {version.Id}",
            Cambios = localizadas > 0
                ? $"Veredicto: {prueba.Veredicto} (con {localizadas} dimensiones localizadas)."
                : $"Veredicto: {prueba.Veredicto}.",
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return prueba;
    }

    // Arma el contexto para los AcidTests dimensionales a partir del canon de la obra.
    // El entity Capitulo no modela presencia ni eventos previos: pasamos el canon
    // disponible y cada test localiza sus hallazgos contra la prosa propuesta.
    private async Task<AcidContext?> ConstruirAcidContextAsync(
        CapituloVersion version, CancellationToken ct)
    {
        var obraId = version.Capitulo?.ObraId ?? Guid.Empty;
        if (obraId == Guid.Empty) return null;

        var obra = await _obras.GetConCanonAsync(obraId, ct);
        if (obra is null) return null;

        var capitulo = obra.Capitulos.FirstOrDefault(c => c.Id == version.CapituloId)
                       ?? version.Capitulo!;

        var presentes = obra.Personajes.OrderBy(p => p.Nombre).ToList();
        var ubicacion = obra.Ubicaciones.OrderBy(u => u.Nombre).FirstOrDefault();
        var eventosPrevios = obra.Eventos.OrderBy(e => e.Orden).ToList();

        // Sin al menos un personaje y una ubicación no hay nada que localizar.
        if (presentes.Count == 0 || ubicacion is null) return null;

        return new AcidContext(capitulo, version.Texto, presentes, ubicacion, eventosPrevios);
    }

    // Vuelca los resultados por dimensión sobre la PruebaAcido holística:
    // actualiza los flags y concatena hallazgos/parches etiquetados.
    private static void FusionarDimensiones(
        PruebaAcido prueba, IReadOnlyList<DimensionResultado> dimensiones)
    {
        var hallazgos = new List<string>();
        var parches = new List<string>();

        foreach (var d in dimensiones)
        {
            var dim = d.Dimension.ToLowerInvariant();
            if (dim.StartsWith("fís") || dim.StartsWith("fis")) prueba.Fisica = d.Resultado.Pasa;
            else if (dim.StartsWith("psic") || dim.StartsWith("psí")) prueba.Psicologica = d.Resultado.Pasa;
            else if (dim.StartsWith("amb")) prueba.Ambiental = d.Resultado.Pasa;
            else if (dim.StartsWith("quí") || dim.StartsWith("qui")) prueba.Quimica = d.Resultado.Pasa;

            if (d.Resultado.Pasa) continue;
            if (!string.IsNullOrWhiteSpace(d.Resultado.Hallazgo))
                hallazgos.Add($"[{d.Dimension}] {d.Resultado.Hallazgo}");
            if (!string.IsNullOrWhiteSpace(d.Resultado.Parche))
                parches.Add($"[{d.Dimension}] {d.Resultado.Parche}");
        }

        if (hallazgos.Count > 0) prueba.Hallazgos = Combinar(prueba.Hallazgos, hallazgos);
        if (parches.Count > 0) prueba.Parches = Combinar(prueba.Parches, parches);
    }

    private static string Combinar(string? baseTexto, List<string> extras)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(baseTexto)) partes.Add(baseTexto!.Trim());
        partes.AddRange(extras);
        return string.Join("\n", partes);
    }

    private static TEnum ParseEnum<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var r) ? r : default;

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
}
