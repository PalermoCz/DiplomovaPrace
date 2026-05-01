namespace DiplomovaPrace.Services;

/// <summary>
/// Abstraction for sending transactional emails (invite, password-reset, etc.).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
