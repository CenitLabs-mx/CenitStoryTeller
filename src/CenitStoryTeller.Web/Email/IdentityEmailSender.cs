using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CenitStoryTeller.Data.Entities;

namespace CenitStoryTeller.Web.Email;

// Puente entre el IEmailSender<Usuario> que Identity inyecta para sus flujos
// (confirmación de cuenta, reset de password, cambio de email) y nuestro
// IAppEmailSender. Mantiene la abstracción de proveedor (Console/SendGrid)
// y deja a Identity construir los cuerpos del email con su plantilla.
public sealed class IdentityEmailSender : IEmailSender<Usuario>
{
    private readonly IAppEmailSender _sender;
    public IdentityEmailSender(IAppEmailSender sender) => _sender = sender;

    public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink) =>
        _sender.EnviarAsync(email, "Confirma tu cuenta en CenitStoryTeller",
            $"""
            <p>Hola,</p>
            <p>Confirma tu cuenta haciendo clic en el siguiente enlace:</p>
            <p><a href="{confirmationLink}">Confirmar cuenta</a></p>
            <p>Si no creaste esta cuenta, ignora este mensaje.</p>
            """);

    public Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink) =>
        _sender.EnviarAsync(email, "Recuperar contraseña de CenitStoryTeller",
            $"""
            <p>Hola,</p>
            <p>Para restablecer tu contraseña, haz clic aquí:</p>
            <p><a href="{resetLink}">Restablecer contraseña</a></p>
            <p>Si no solicitaste el reset, ignora este mensaje y tu contraseña seguirá igual.</p>
            """);

    public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode) =>
        _sender.EnviarAsync(email, "Código de recuperación de CenitStoryTeller",
            $"<p>Tu código para restablecer la contraseña es: <strong>{resetCode}</strong></p>");
}
