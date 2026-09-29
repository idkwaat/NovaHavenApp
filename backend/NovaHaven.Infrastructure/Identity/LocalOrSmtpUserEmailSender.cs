using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NovaHaven.Application.Features.Account;

namespace NovaHaven.Infrastructure.Identity;

public sealed class LocalOrSmtpUserEmailSender(
    IConfiguration configuration,
    IHostEnvironment environment) : IUserEmailSender, ILocalEmailOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public bool IsConfigured => environment.IsDevelopment() ||
        (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) &&
         !string.IsNullOrWhiteSpace(configuration["Email:Smtp:From"]));
    private string OutboxPath => configuration["Email:LocalOutboxPath"] is { Length: > 0 } configured
        ? Path.GetFullPath(configured)
        : Path.Combine(environment.ContentRootPath, ".local", "mail-outbox");

    public async Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:Smtp:Host"];
        var from = configuration["Email:Smtp:From"];
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(from))
        {
            var port = configuration.GetValue("Email:Smtp:Port", 587);
            var useSsl = configuration.GetValue("Email:Smtp:UseSsl", true);
            using var message = new MailMessage(from, email)
            {
                Subject = "Xác nhận tài khoản Nova Haven",
                Body = $"Chào bạn,\r\n\r\nMở liên kết sau để xác nhận email Nova Haven:\r\n{confirmationUrl}\r\n\r\nNếu bạn không tạo tài khoản, hãy bỏ qua thư này.",
                IsBodyHtml = false
            };
            using var client = new SmtpClient(host, port) { EnableSsl = useSsl };
            var username = configuration["Email:Smtp:Username"];
            var password = configuration["Email:Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
            await client.SendMailAsync(message, cancellationToken);
            return;
        }

        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Email delivery is not configured. Set Email:Smtp:Host and Email:Smtp:From.");

        Directory.CreateDirectory(OutboxPath);
        var entry = new UserEmailMessage(Guid.NewGuid(), email, "Xác nhận tài khoản Nova Haven", confirmationUrl, DateTimeOffset.UtcNow);
        var path = Path.Combine(OutboxPath, $"{entry.Id:N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(entry, JsonOptions), cancellationToken);
    }

    public async Task<IReadOnlyList<UserEmailMessage>> GetLatestForRecipientAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("The local email outbox is available only in Development.");
        if (!Directory.Exists(OutboxPath)) return Array.Empty<UserEmailMessage>();

        var results = new List<UserEmailMessage>();
        foreach (var path in Directory.EnumerateFiles(OutboxPath, "*.json")
                     .OrderByDescending(File.GetLastWriteTimeUtc).Take(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(path, cancellationToken);
                var entry = JsonSerializer.Deserialize<UserEmailMessage>(json, JsonOptions);
                if (entry is not null && string.Equals(entry.Recipient, email, StringComparison.OrdinalIgnoreCase)) results.Add(entry);
            }
            catch (JsonException) { }
        }
        return results.OrderByDescending(x => x.CreatedAtUtc).Take(20).ToArray();
    }
}
