using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NovelaEngine.Data.Services;

public interface IPromptProvider
{
    Task<string> MotorDeHistoriaAsync(CancellationToken ct = default);
    Task<string> ContinuidadAcidoAsync(CancellationToken ct = default);
    Task<string> ModernizarAsync(CancellationToken ct = default);
}

// Lee los system-prompts versionados en el repo (carpeta prompts/).
public sealed class FilePromptProvider : IPromptProvider
{
    private readonly string _baseDir;
    public FilePromptProvider(IConfiguration config) =>
        _baseDir = config["Prompts:Directorio"] ?? "prompts";

    public Task<string> MotorDeHistoriaAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "motor-de-historia.md"), ct);

    public Task<string> ContinuidadAcidoAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "continuidad-acido.md"), ct);

    public Task<string> ModernizarAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "moderniza.md"), ct);
}
