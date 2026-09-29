namespace NovaHaven.Application.Features.Account;

public sealed record UserEmailMessage(Guid Id, string Recipient, string Subject, string ConfirmationUrl, DateTimeOffset CreatedAtUtc);

public interface IUserEmailSender
{
    bool IsConfigured { get; }
    Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default);
}

public interface ILocalEmailOutbox
{
    Task<IReadOnlyList<UserEmailMessage>> GetLatestForRecipientAsync(string email, CancellationToken cancellationToken = default);
}
