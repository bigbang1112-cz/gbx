﻿using Microsoft.AspNetCore.Authentication;

namespace BigBang1112.Gbx.Server.Endpoints;

public class LoginEndpoint : IEndpoint
{
    public void Endpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("login", Login);
    }

    private static async Task Login(HttpContext httpContext, string? redirectUri)
    {
        var location = httpContext.Request.Headers.Location.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(location))
        {
            location = redirectUri ?? "/";
        }

        await httpContext.ChallengeAsync(new AuthenticationProperties { RedirectUri = location });
    }
}
