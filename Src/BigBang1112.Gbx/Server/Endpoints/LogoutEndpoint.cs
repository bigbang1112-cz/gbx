using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BigBang1112.Gbx.Server.Endpoints;

public class LogoutEndpoint : IEndpoint
{
    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("logout", Logout);
    }

    private static async Task Logout(HttpContext httpContext, string? redirectUri)
    {
        var location = httpContext.Request.Headers.Location.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(location))
        {
            location = redirectUri ?? "/";
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = location });
    }
}
