using Microsoft.AspNetCore.Identity;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Auth.Repositories;
using NovaHaven.Application.Features.Auth.Results;

namespace NovaHaven.Infrastructure.Identity;

public sealed class IdentityUserAccountRepository(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn) : IUserAccountRepository
{
    public async Task<ApplicationResult<UserAccountResult>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return ApplicationResult<UserAccountResult>.Failure(new ApplicationError(
                    "account.email-taken",
                    "Email đã được đăng ký"));
            }

            var messages = result.Errors.Select(MapIdentityError)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            IReadOnlyDictionary<string, string[]> errors = new Dictionary<string, string[]>
            {
                ["account"] = messages
            };
            return ApplicationResult<UserAccountResult>.Failure(new ApplicationError(
                "validation.failed", "One or more validation errors occurred.", errors));
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return ApplicationResult<UserAccountResult>.Success(ToResult(user, isAdmin: false));
    }

    public async Task<bool> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null) return false;

        var result = await signIn.PasswordSignInAsync(
            user, password, isPersistent: false, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public async Task<UserAccountResult?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString());
        return user is null ? null : ToResult(user, await users.IsInRoleAsync(user, "Admin"));
    }

    public Task SignOutAsync(CancellationToken cancellationToken) => signIn.SignOutAsync();

    private static UserAccountResult ToResult(ApplicationUser user, bool isAdmin) =>
        new(user.Id, user.Email, isAdmin);

    private static string MapIdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "Mật khẩu cần ít nhất 12 ký tự.",
        "PasswordTooLong" => "Mật khẩu quá dài.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần ít nhất một ký tự đặc biệt.",
        "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất một chữ số.",
        "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất một chữ hoa.",
        "PasswordRequiresLower" => "Mật khẩu cần có ít nhất một chữ thường.",
        "InvalidEmail" => "Địa chỉ email không hợp lệ.",
        _ => "Thông tin tài khoản không đáp ứng chính sách bảo mật."
    };
}
