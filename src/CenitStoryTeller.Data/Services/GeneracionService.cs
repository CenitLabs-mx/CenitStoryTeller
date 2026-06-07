using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

}

public sealed class GeneracionService : IGeneracionService
{
    private readonly ILlmClientFactory _llmFactory;
    private readonly ILlmOptionsAccessor _opts;
    private readonly IPromptProvider _prompts;
    private readonly ICapituloRepository _capitulos;
    private readonly IObraRepository _obras;
    private readonly IRegistroPasoRepository _pasos;
    private readonly IUnitOfWork _uow;
    private readonly IAcidTestRunner _acidTests;

    public GeneracionService(
        ILlmClientFactory llmFactory,
        ILlmOptionsAccessor opts,
        IPromptProvider prompts,
        ICapituloRepository capitulos,
        IObraRepository obras,
        IRegistroPasoRepository pasos,
        IUnitOfWork uow,
        IAcidTestRunner acidTests)
    {
        _llmFactory = llmFactory;
        _opts = opts;
        _prompts = prompts;
        _capitulos = capitulos;
        _obras = obras;
        _pasos = pasos;
        _uow = uow;
        _acidTests = acidTests;
    }

    public async Task<CapituloVersion> GenerarBorradorAsync(
        Guid capituloId, string promptUsuario, CancellationToken ct = default)
    {
        var capitulo = await _capitulos.GetAsync(capituloId, ct)
            ?? throw new InvalidOperationException($"No existe el capítulo {capituloId}.");

        var system = await _prompts.MotorDeHistoriaAsync(ct);
        var opt = await _opts.ObtenerAsync(ct);
        var llm = await _llmFactory.ObtenerAsync(ct);
        var resp = await llm.CompleteAsync(new LlmRequest
        {
            Model = opt.ModelDraft,          // borrador con modelo barato
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
        var opt = await _opts.ObtenerAsync(ct);
        var llm = await _llmFactory.ObtenerAsync(ct);
        var resp = await llm.CompleteAsync(new LlmRequest
        {
            Model = opt.ModelReview,
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
            switch (d.Dimension)
            {
                case AcidDimension.Fisica: prueba.Fisica = d.Resultado.Pasa; break;
                case AcidDimension.Psicologica: prueba.Psicologica = d.Resultado.Pasa; break;
                case AcidDimension.Ambiental: prueba.Ambiental = d.Resultado.Pasa; break;
                case AcidDimension.Quimica: prueba.Quimica = d.Resultado.Pasa; break;
            }

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

}
