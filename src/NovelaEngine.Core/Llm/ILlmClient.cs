using System.Threading;
using System.Threading.Tasks;

namespace NovelaEngine.Core.Llm;

public interface ILlmClient
{
    Task<string> CompletarPromptAsync(string prompt, string modelRole, CancellationToken ct);
}
