using System.Security.Claims;

namespace BigBang1112.Gbx.Server.Endpoints.API.V1;

public class IdentityEndpoint : IEndpoint
{
    private readonly ILogger<IdentityEndpoint> _logger;

    public IdentityEndpoint(ILogger<IdentityEndpoint> logger)
    {
        _logger = logger;
    }

    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("identity", (HttpContext httpContext) => Results.Ok(Identity(httpContext)));
    }

    private static Dictionary<string, List<string>> Identity(HttpContext httpContext)
    {
        if (httpContext.User.Identity is not ClaimsIdentity identity)
        {
            return new Dictionary<string, List<string>>();
        }

        return identity.Claims
            .ToLookup(x => x.Type, x => x.Value)
            .ToDictionary(x => x.Key, x => x.ToList());
    }
}
