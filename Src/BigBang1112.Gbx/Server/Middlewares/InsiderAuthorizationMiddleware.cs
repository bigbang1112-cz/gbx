using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using System.Security.Principal;

namespace BigBang1112.Gbx.Server.Middlewares;

public class InsiderAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public InsiderAuthorizationMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isInsiderMode = _config.GetValue<bool>("InsiderMode");

        if (!isInsiderMode || await PassInsiderAsync(context))
        {
            await _next(context);
        }
    }

    internal async Task<bool> PassInsiderAsync(HttpContext context)
    {
        if (context.User.Identity is not ClaimsIdentity identity || !context.User.Identity.IsAuthenticated)
        {
            await context.ChallengeAsync();
            return false;
        }

        if (IsValidInsider(identity))
        {
            return true;
        }

        context.Response.StatusCode = 403;

        return false;
    }

    internal bool IsValidInsider(ClaimsIdentity identity)
    {
        var nameIdentifier = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (nameIdentifier is null)
        {
            return false;
        }

        var insiders = _config.GetSection("Insiders").Get<HashSet<string>>();

        if (insiders is null)
        {
            return false;
        }

        return insiders.Contains(nameIdentifier);
    }
}
