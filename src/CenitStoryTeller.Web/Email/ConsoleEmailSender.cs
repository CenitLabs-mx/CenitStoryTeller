using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CenitStoryTeller.Web.Email;

// Para desarrollo local: imprime el email completo a los logs. Útil para
// recuperar el link de confirmación de registro sin SMTP configurado.
public sealed class ConsoleEmailSender : IAppEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _log;
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> log) => _log = log;

    public Task EnviarAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _log.LogWarning(
            "=== [DEV EMAIL — NO SMTP CONFIGURADO] ===\n" +
            "To:      {To}\nSubject: {Subject}\n\n{Body}\n=========================================",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
