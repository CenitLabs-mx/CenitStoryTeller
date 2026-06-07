using System.Threading;
using System.Threading.Tasks;

namespace CenitStoryTeller.Core.Llm;

// Devuelve la LlmOptions efectiva para esta solicitud. La implementación en Data
// elige la config del usuario autenticado (si existe) y cae a la config global de
// appsettings (que es lo que se inyecta por defecto en este accessor) en cualquier
// otro caso: --migrate, seeder, llamada en background sin usuario.
public interface ILlmOptionsAccessor
{
    Task<LlmOptions> ObtenerAsync(CancellationToken ct = default);
}
