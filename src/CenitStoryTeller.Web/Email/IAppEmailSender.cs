using System.Threading;
using System.Threading.Tasks;

namespace CenitStoryTeller.Web.Email;

// Abstracción del envío de email. Identity tiene su propio IEmailSender (en
// Microsoft.AspNetCore.Identity.UI) pero ese asume Razor Pages scaffolded.
// Usamos el nuestro y lo conectamos a Identity vía Microsoft.AspNetCore.Identity.IEmailSender<TUser>.
public interface IAppEmailSender
{
    Task EnviarAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
