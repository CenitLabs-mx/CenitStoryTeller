using Microsoft.AspNetCore.Identity;

namespace CenitStoryTeller.Data.Entities;

// Usuario de la aplicación. Extiende IdentityUser<Guid> para que el Id sea Guid
// (mismo tipo que usan el resto de las entidades del dominio) y poder ligar
// Obra.UsuarioId sin conversión.
public sealed class Usuario : IdentityUser<Guid>
{
    public string? NombreDisplay { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
