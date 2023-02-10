using GBX.NET.Inputs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

    public async Task InvokeAsync(HttpContext context, IAuthorizationService authorizationService)
    {
        if (await IsValidInsiderAsync(context, authorizationService))
        {
            await _next(context);
            return;
        }
        
        if (NotAuthenticated(context.User))
        {
            await context.ChallengeAsync();
            return;
        }
        
        context.Response.StatusCode = 403;
    }

    internal async ValueTask<bool> IsValidInsiderAsync(HttpContext context, IAuthorizationService authorizationService)
    {
        var isInsiderMode = _config.GetValue<bool>("InsiderMode");

        if (!isInsiderMode)
        {
            return true;
        }

        var user = context.User;

        if (NotAuthenticated(user))
        {
            var result = await authorizationService.AuthorizeAsync(user, "InsiderPolicy");

            if (!result.Succeeded)
            {
                return false;
            }
        }

        var nameIdentifier = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

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

    private static bool NotAuthenticated(ClaimsPrincipal user)
    {
        return user.Identity is null || !user.Identity.IsAuthenticated;
    }
}
