using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NovelaEngine.Core.Llm;
using NovelaEngine.Data.Entities;
using NovelaEngine.Data.Repositories;

namespace NovelaEngine.Data.Services;

public interface IGeneracionService
{
    // Escribe un borrador del capítulo con ModelDraft y lo guarda como nueva versión.
    Task<CapituloVersion> GenerarBorradorAsync(Guid capituloId, string promptUsuario, CancellationToken ct = default);

    // Corre la prueba del ácido sobre una versión con ModelReview y guarda el veredicto.
    Task<PruebaAcido> CorrerPruebaAcidoAsync(Guid capituloVersionId, CancellationToken ct = default);
}

public sealed class GeneracionService : IGeneracionService
{
    private readonly ILlmClient _llm;
    private readonly LlmOptions _opt;
    private readonly IPromptProvider _prompts;
    private readonly ICapituloRepository _capitulos;
    private readonly IRegistroPasoRepository _pasos;
    private readonly IUnitOfWork _uow;

    public GeneracionService(
        ILlmClient llm,
        IOptions<LlmOptions> opt,
        IPromptProvider prompts,
        ICapituloRepository capitulos,
        IRegistroPasoRepository pasos,
        IUnitOfWork uow)
    {
        _llm = llm;
        _opt = opt.Value;
        _prompts = prompts;
        _capitulos = capitulos;
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
}
