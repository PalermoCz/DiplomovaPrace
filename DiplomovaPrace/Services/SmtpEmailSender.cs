using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiplomovaPrace.Services;

/// <summary>
/// SMTP-based email sender. Reads configuration from the "Email" section of appsettings.
///
/// Required appsettings keys (appsettings.Local.json for local overrides):
///   Email:SmtpHost    — SMTP server hostname (e.g. "smtp.sendgrid.net")
///   Email:SmtpPort    — SMTP port (e.g. 587)
///   Email:From        — Sender address (e.g. "noreply@yourdomain.com")
///   Email:Username    — SMTP username (optional; leave empty for unauthenticated relay)
///   Email:Password    — SMTP password (optional)
///   Email:EnableSsl   — true/false
///
/// If SmtpHost is not configured the service logs a warning and skips delivery silently —
/// this allows the invite flow to work in dev without a real SMTP server (token is still saved
/// to the DB and can be copied from logs).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger, IWebHostEnvironment environment)
    {
        _config = config;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _config["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning(
                    "Email:SmtpHost is not configured in Development. Skipping email delivery to {To}. Subject: {Subject}",
                    to, subject);
                return;
            }

            throw new InvalidOperationException("Email delivery is not configured. Set Email:SmtpHost for non-development environments.");
        }

        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var from = _config["Email:From"] ?? "noreply@facility.local";
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var enableSsl = string.Equals(_config["Email:EnableSsl"], "true", StringComparison.OrdinalIgnoreCase);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000
        };

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(from, to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }
}
