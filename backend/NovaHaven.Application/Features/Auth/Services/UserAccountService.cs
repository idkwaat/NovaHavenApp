using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Auth.Repositories;
using NovaHaven.Application.Features.Auth.Results;

namespace NovaHaven.Application.Features.Auth.Services;

public sealed class UserAccountService(IUserAccountRepository repository)
{
    public Task<ApplicationResult<UserAccountResult>> RegisterAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail)
            || normalizedEmail.Length > 256
            || string.IsNullOrWhiteSpace(password)
            || password.Length > 128)
        {
            IReadOnlyDictionary<string, string[]> errors = new Dictionary<string, string[]>
            {
                ["email"] = ["Email hoặc mật khẩu không hợp lệ."]
            };
            return Task.FromResult(ApplicationResult<UserAccountResult>.Failure(
                new ApplicationError("validation.failed", "One or more validation errors occurred.", errors)));
        }

        return repository.RegisterAsync(normalizedEmail, password, cancellationToken);
    }

    public Task<bool> SignInAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult(false);

        return repository.SignInAsync(email.Trim(), password, cancellationToken);
    }

    public Task<UserAccountResult?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.FindByIdAsync(id, cancellationToken);

    public Task SignOutAsync(CancellationToken cancellationToken) => repository.SignOutAsync(cancellationToken);
}
