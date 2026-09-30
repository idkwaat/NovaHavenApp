using Microsoft.AspNetCore.Antiforgery;

namespace NovaHaven.Api.Security;

public static class AntiforgeryRequestValidator
{
    public static async Task<bool> IsValidAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
