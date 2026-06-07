using System.Threading;
using System.Threading.Tasks;

namespace CenitStoryTeller.Core.Llm;

// Construye un ILlmClient con la LlmOptions efectiva (resuelta vía ILlmOptionsAccessor).
// Inyectarlo en los servicios y llamar ObtenerAsync una vez al inicio de la operación;
// el cliente puede reusarse durante esa operación. Evita el sync-over-async que tendría
// inyectar ILlmClient directamente cuando su construcción depende de una query a BD.
public interface ILlmClientFactory
{
    Task<ILlmClient> ObtenerAsync(CancellationToken ct = default);
}
