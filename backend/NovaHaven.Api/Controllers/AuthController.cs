using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Auth;
using NovaHaven.Api.Errors;
using NovaHaven.Api.Security;
using NovaHaven.Application.Features.Auth.Results;
using NovaHaven.Application.Features.Auth.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(UserAccountService userAccountService) : ControllerBase
{
    [HttpGet("csrf")]
    [ProducesResponseType(typeof(CsrfTokenResponse), StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new CsrfTokenResponse(antiforgery.GetAndStoreTokens(HttpContext).RequestToken));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        [FromServices] IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await AntiforgeryRequestValidator.IsValidAsync(HttpContext, antiforgery))
            return InvalidCsrf();

        var result = await userAccountService.RegisterAsync(request.Email, request.Password, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error!.Code == "account.email-taken")
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Email đã được đăng ký",
                    detail: "Hãy đăng nhập hoặc dùng địa chỉ email khác.");
            }

            return ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        }

        return Created("/api/v1/auth/me", ToResponse(result.Value!));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        [FromServices] IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await AntiforgeryRequestValidator.IsValidAsync(HttpContext, antiforgery))
            return InvalidCsrf();

        return await userAccountService.SignInAsync(request.Email, request.Password, cancellationToken)
            ? NoContent()
            : Unauthorized();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CurrentUser(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var user = await userAccountService.FindByIdAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(ToResponse(user));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromServices] IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (!await AntiforgeryRequestValidator.IsValidAsync(HttpContext, antiforgery))
            return InvalidCsrf();

        await userAccountService.SignOutAsync(cancellationToken);
        return NoContent();
    }

    private IActionResult InvalidCsrf() =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid anti-forgery token.");

    private static AuthenticatedUserResponse ToResponse(UserAccountResult user) =>
        new(user.Id.ToString(), user.Email, user.IsAdmin);
}
