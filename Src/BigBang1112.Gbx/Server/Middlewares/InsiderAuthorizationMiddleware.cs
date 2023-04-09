using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace BigBang1112.Gbx.Server.Middlewares;

public class InsiderAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public InsiderAuthorizationMiddleware(RequestDelegate next, IConfiguration config, IMemoryCache cache)
    {
        _next = next;
        _config = config;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isInsiderMode = _config.GetValue<bool>(Constants.InsiderMode);

        if (!isInsiderMode || PassInsider(context))
        {
            await _next(context);
            return;
        }

        switch (context.Request.Path)
        {
            case "/":
                await ShowTeaserPage(context);
                break;
            case "/login":
                await context.ChallengeAsync(new AuthenticationProperties { RedirectUri = "/" });
                break;
            default:
                context.Response.StatusCode = 404;
                break;
        }
    }

    private async Task ShowTeaserPage(HttpContext context)
    {
        var html = await _cache.GetOrCreateAsync("JoinPage", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return await File.ReadAllTextAsync("SpecialPages/Join.html");
        }) ?? throw new Exception();

        var authenticated = context.User.Identity is ClaimsIdentity identity && context.User.Identity.IsAuthenticated;

        html = string.Format(html, authenticated ? "<div class=\"join\">Joined</div>" : "<a href=\"/login\" class=\"join\">Join</a>");

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html);
    }

    internal bool PassInsider(HttpContext context)
    {
        if (context.User.Identity is not ClaimsIdentity identity || !context.User.Identity.IsAuthenticated)
        {
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
