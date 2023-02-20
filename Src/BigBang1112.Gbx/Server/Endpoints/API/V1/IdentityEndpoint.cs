using BigBang1112.Gbx.Shared;
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
        app.MapGet("identity", Identity)
            .RequireAuthorization();
    }

    private static IResult Identity(HttpContext httpContext)
    {
        if (httpContext.User.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var model = new Identity
        {
            AuthenticationType = identity.AuthenticationType,
            Claims = identity.Claims
                .ToLookup(x => x.Type, x => x.Value)
                .ToDictionary(x => x.Key, x => x.ToList())
        };

        return Results.Ok(model);
    }
}
