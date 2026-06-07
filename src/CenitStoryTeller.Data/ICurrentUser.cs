using System;

namespace CenitStoryTeller.Data;

// Accesor del usuario autenticado actual. Inyectado en el DbContext para que
// los query filters multi-tenant ("mis obras + demos") puedan referenciarlo,
// y en los repositorios que necesitan estampar UsuarioId al insertar.
public interface ICurrentUser
{
    // Null si no hay sesión activa (request anónima, background task, test).
    Guid? Id { get; }
    bool IsAuthenticated => Id is not null;
}

// Implementación por defecto para escenarios sin HTTP (tests, --migrate, seeders):
// no hay usuario. La implementación web vive en CenitStoryTeller.Web y se
// registra en Program.cs.
public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid? Id => null;
}
