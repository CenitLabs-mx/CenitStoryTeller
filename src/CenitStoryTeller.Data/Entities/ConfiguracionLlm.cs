using System;

namespace CenitStoryTeller.Data.Entities;

// Configuración LLM por usuario. 1:1 con Usuario. Si un usuario no tiene fila,
// se cae a la configuración global de appsettings (modo legacy / dev).
public sealed class ConfiguracionLlm
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    // openai | gemini | ollama
    public string Provider { get; set; } = "openai";

    // En BD por ahora en texto plano. Si el repo se vuelve público con BD compartida,
    // habrá que cifrar at-rest (DPAPI / clave en env). Issue para 6.2.x.
    public string ApiKey { get; set; } = "";

    public string? BaseUrl { get; set; }
    public string ModelDraft { get; set; } = "gpt-4o-mini";
    public string ModelReview { get; set; } = "gpt-4o";

    public DateTimeOffset ActualizadaEn { get; set; } = DateTimeOffset.UtcNow;
}
