using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using NovaHaven.Application.Features.Account;
using NovaHaven.Infrastructure.Identity;

namespace NovaHaven.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record LoginInput(string Email, string Password);
    public sealed record RegisterInput(string Email, string Password);
    public sealed record ConfirmEmailInput(string Email, string Token);
    public sealed record ResendConfirmationInput(string Email);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");
        group.MapGet("/csrf", (HttpContext http, IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = antiforgery.GetAndStoreTokens(http).RequestToken });
        });

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/resend-confirmation", ResendConfirmationAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/me", CurrentUserAsync).RequireAuthorization();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/api/v1/dev/mailbox", async (string email, ILocalEmailOutbox outbox, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "no-store";
                return Results.Ok(await outbox.GetLatestForRecipientAsync(email, ct));
            }).WithTags("Development");
        }
    }

    private static async Task<IResult> RegisterAsync(HttpContext http, IAntiforgery csrf,
        UserManager<ApplicationUser> users, IUserEmailSender emailSender, IConfiguration configuration,
        RegisterInput input, CancellationToken cancellationToken)
    {
        if (!await ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!emailSender.IsConfigured) return EmailUnavailable();
        var email = input.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length > 128)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Email hoặc mật khẩu không hợp lệ."] });

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName")) return Results.Accepted();
            return Results.ValidationProblem(new Dictionary<string, string[]> {
                ["account"] = result.Errors.Select(error => error.Code switch
                {
                    "PasswordTooShort" => "Mật khẩu cần ít nhất 12 ký tự.",
                    "PasswordTooLong" => "Mật khẩu quá dài.",
                    "PasswordRequiresNonAlphanumeric" => "Mật khẩu cần ít nhất một ký tự đặc biệt.",
                    "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất một chữ số.",
                    "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất một chữ hoa.",
                    "PasswordRequiresLower" => "Mật khẩu cần có ít nhất một chữ thường.",
                    "InvalidEmail" => "Địa chỉ email không hợp lệ.",
                    _ => "Thông tin tài khoản không đáp ứng chính sách bảo mật."
                }).Distinct().ToArray()
            });
        }

        return await SendConfirmationAsync(configuration, users, emailSender, user, cancellationToken);
    }

    private static async Task<IResult> ConfirmEmailAsync(HttpContext http, IAntiforgery csrf,
        UserManager<ApplicationUser> users, ConfirmEmailInput input)
    {
        if (!await ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Token))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["Liên kết xác nhận không hợp lệ hoặc đã hết hạn."] });
        var user = await users.FindByEmailAsync(input.Email.Trim());
        if (user is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["Liên kết xác nhận không hợp lệ hoặc đã hết hạn."] });
        var result = await users.ConfirmEmailAsync(user, input.Token);
        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["Liên kết xác nhận không hợp lệ hoặc đã hết hạn."] });
    }

    private static async Task<IResult> ResendConfirmationAsync(HttpContext http, IAntiforgery csrf,
        UserManager<ApplicationUser> users, IUserEmailSender emailSender, IConfiguration configuration,
        ResendConfirmationInput input, CancellationToken cancellationToken)
    {
        if (!await ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!emailSender.IsConfigured) return EmailUnavailable();
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 256) return Results.Accepted();
        var user = await users.FindByEmailAsync(input.Email.Trim());
        if (user is null || user.EmailConfirmed) return Results.Accepted();
        return await SendConfirmationAsync(configuration, users, emailSender, user, cancellationToken);
    }

    private static async Task<IResult> SendConfirmationAsync(IConfiguration configuration,
        UserManager<ApplicationUser> users, IUserEmailSender emailSender, ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var origin = configuration["Email:PublicWebOrigin"] ?? "http://localhost:3002";
        var confirmationUrl = $"{origin.TrimEnd('/')}/account?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
        try
        {
            await emailSender.SendConfirmationAsync(user.Email!, confirmationUrl, cancellationToken);
            return Results.Accepted();
        }
        catch (InvalidOperationException)
        {
            return EmailUnavailable();
        }
    }

    private static IResult EmailUnavailable() => Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Email chưa được cấu hình", detail: "Trong môi trường local, chạy API với Development để dùng hộp thư local; môi trường khác cần cấu hình SMTP.");

    private static async Task<IResult> LoginAsync(HttpContext http, IAntiforgery csrf,
        UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, LoginInput input)
    {
        if (!await ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Results.Unauthorized();
        var user = await users.FindByEmailAsync(input.Email.Trim());
        if (user is null) return Results.Unauthorized();
        var result = await signIn.PasswordSignInAsync(user, input.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded) return Results.NoContent();
        if (result.IsNotAllowed && !user.EmailConfirmed)
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Email chưa được xác nhận", detail: "Hãy mở thư xác nhận hoặc gửi lại liên kết xác nhận.");
        return Results.Unauthorized();
    }

    private static async Task<IResult> CurrentUserAsync(HttpContext http, UserManager<ApplicationUser> users)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return Results.Unauthorized();
        return Results.Ok(new { id = user.Id.ToString(), email = user.Email, emailConfirmed = user.EmailConfirmed, isAdmin = await users.IsInRoleAsync(user, "Admin") });
    }

    private static async Task<IResult> LogoutAsync(HttpContext http, IAntiforgery csrf, SignInManager<ApplicationUser> signIn)
    {
        if (!await ValidateCsrf(http, csrf)) return InvalidCsrf();
        await signIn.SignOutAsync();
        return Results.NoContent();
    }

    private static IResult InvalidCsrf() => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid anti-forgery token.");

    public static async Task<bool> ValidateCsrf(HttpContext http, IAntiforgery csrf)
    {
        try { await csrf.ValidateRequestAsync(http); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
