using BigBang1112.Gbx.Server.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BigBang1112.Gbx.Server.Middlewares;

public class RegularAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly IOptions<DiscordOptions> _discordOptions;

    public RegularAuthorizationMiddleware(RequestDelegate next, IConfiguration config, IOptions<DiscordOptions> discordOptions)
    {
        _next = next;
        _config = config;
        _discordOptions = discordOptions;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        AddAdditionalClaimsAsync(httpContext);

        await _next(httpContext);
    }
    
    private void AddAdditionalClaimsAsync(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == false || httpContext.User.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var claims = identity.Claims.ToLookup(x => x.Type, x => x.Value);

        var nameIdentifier = claims[ClaimTypes.NameIdentifier].FirstOrDefault();

        if (nameIdentifier is null || identity.AuthenticationType is null)
        {
            return;
        }

        if (_discordOptions.Value.OwnerSnowflake == nameIdentifier)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        var insiders = _config.GetSection("Insiders").Get<HashSet<string>>();

        if (insiders?.Contains(nameIdentifier) == true)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Insider"));
        }

        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
    }
}
