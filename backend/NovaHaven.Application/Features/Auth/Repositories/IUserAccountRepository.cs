using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Auth.Results;

namespace NovaHaven.Application.Features.Auth.Repositories;

public interface IUserAccountRepository
{
    Task<ApplicationResult<UserAccountResult>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<bool> SignInAsync(string email, string password, CancellationToken cancellationToken);

    Task<UserAccountResult?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
