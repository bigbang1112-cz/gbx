using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BigBang1112.Gbx.Shared;

public static class SharedOptions
{
    public static void Authorization(AuthorizationOptions options)
    {
        options.AddPolicy(Constants.SuperAdminPolicy, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(ClaimTypes.Role, Constants.SuperAdmin);
        });
    }
}
