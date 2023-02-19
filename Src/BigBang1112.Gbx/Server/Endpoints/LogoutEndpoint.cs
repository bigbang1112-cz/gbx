using Microsoft.AspNetCore.Authentication;

namespace BigBang1112.Gbx.Server.Endpoints;

public class LogoutEndpoint : IEndpoint
{
    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("logout", Logout);
    }

    private static async Task Logout(HttpContext httpContext)
    {
        var location = httpContext.Request.Headers.Location.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(location))
        {
            location = "/";
        }

        await httpContext.SignOutAsync(Constants.Cookies, new AuthenticationProperties { RedirectUri = location });
    }
}
