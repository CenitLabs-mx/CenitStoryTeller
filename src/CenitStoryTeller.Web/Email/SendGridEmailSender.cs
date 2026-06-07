using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CenitStoryTeller.Web.Email;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";
    public string ApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "noreply@cenit-labs.com";
    public string FromName { get; set; } = "CenitStoryTeller";
}

public sealed class SendGridEmailSender : IAppEmailSender
{
    private readonly SendGridOptions _opt;
    private readonly ILogger<SendGridEmailSender> _log;

    public SendGridEmailSender(IOptions<SendGridOptions> opt, ILogger<SendGridEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task EnviarAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
            throw new InvalidOperationException(
                "SendGrid:ApiKey no está configurado. Usa el ConsoleEmailSender en dev o setea SendGrid__ApiKey.");

        var client = new SendGridClient(_opt.ApiKey);
        var from = new EmailAddress(_opt.FromEmail, _opt.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

        var resp = await client.SendEmailAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Body.ReadAsStringAsync(ct);
            _log.LogError("SendGrid devolvió {Status}: {Body}", resp.StatusCode, body);
            throw new InvalidOperationException(
                $"SendGrid respondió {resp.StatusCode} al enviar a {toEmail}.");
        }
    }
}
