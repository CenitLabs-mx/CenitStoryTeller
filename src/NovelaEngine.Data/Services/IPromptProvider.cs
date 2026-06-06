using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NovelaEngine.Data.Services;

public interface IPromptProvider
{
    Task<string> MotorDeHistoriaAsync(CancellationToken ct = default);
    Task<string> ContinuidadAcidoAsync(CancellationToken ct = default);
    Task<string> ModernizaAsync(CancellationToken ct = default);
}

// Lee los system-prompts versionados en el repo (carpeta prompts/).
public sealed class FilePromptProvider : IPromptProvider
{
    private readonly string _baseDir;
    public FilePromptProvider(IConfiguration config)
    {
        var dir = config["Prompts:Directorio"] ?? "prompts";
        if (Path.IsPathRooted(dir))
        {
            _baseDir = dir;
        }
        else
        {
            // Search in typical locations:
            // 1. Current directory
            var path1 = Path.Combine(Directory.GetCurrentDirectory(), dir);
            // 2. Parent directory (e.g. running from project folder)
            var path2 = Path.Combine(Directory.GetCurrentDirectory(), "..", dir);
            // 3. Grandparent directory (e.g. running from src/NovelaEngine.Web)
            var path3 = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", dir);
            // 4. Base directory (e.g. compiled output or publish)
            var path4 = Path.Combine(AppContext.BaseDirectory, dir);

            if (Directory.Exists(path1)) _baseDir = path1;
            else if (Directory.Exists(path2)) _baseDir = path2;
            else if (Directory.Exists(path3)) _baseDir = path3;
            else _baseDir = path4; // Fallback to base directory
        }
    }

    public Task<string> MotorDeHistoriaAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "motor-de-historia.md"), ct);

    public Task<string> ContinuidadAcidoAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "continuidad-acido.md"), ct);

    public Task<string> ModernizaAsync(CancellationToken ct = default) =>
        File.ReadAllTextAsync(Path.Combine(_baseDir, "moderniza.md"), ct);
}
