namespace CenitStoryTeller.Core.Llm;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; set; } = "openai";   // openai | gemini | ollama
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; }                // override de endpoint (Ollama, Azure, proxy)
    public string ModelDraft { get; set; } = "gpt-4o-mini";   // borradores: modelo barato
    public string ModelReview { get; set; } = "gpt-4o";       // prueba del ácido: modelo fuerte
    public double Temperature { get; set; } = 0.8;
}
