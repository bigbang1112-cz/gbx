using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BigBang1112.Gbx.Server;

static class DiscordAuthentication
{
    internal static async Task TicketReceived(TicketReceivedContext context)
    {
        var principal = context.Principal;

        if (principal is null || principal.Identity?.IsAuthenticated == false)
        {
            return;
        }

        var snowflakeStr = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(snowflakeStr))
        {
            return;
        }

        var snowflake = ulong.Parse(snowflakeStr);

        var name = principal.FindFirstValue(ClaimTypes.Name) ?? throw new Exception("Name claim not found");
        var discriminator = int.Parse(principal.FindFirstValue(DiscordAuthenticationConstants.Claims.Discriminator) ?? throw new Exception("Discriminator claim not found")); ;
        var avatarHash = principal.FindFirstValue(DiscordAuthenticationConstants.Claims.AvatarHash) ?? throw new Exception("AvatarHash claim not found");

        var uow = context.HttpContext.RequestServices.GetRequiredService<IGbxUnitOfWork>();

        var member = await uow.Members.GetBySnowflakeAsync(snowflake);

        if (member is null)
        {
            member = new Models.Member
            {
                Snowflake = snowflake,
                Name = name,
                Discriminator = discriminator,
                AvatarHash = avatarHash
            };

            await uow.Members.AddAsync(member);
            await uow.SaveAsync();

            return;
        }

        var anythingChanged = false;

        if (member.Name != name)
        {
            member.Name = name;
            anythingChanged = true;
        }

        if (member.Discriminator != discriminator)
        {
            member.Discriminator = discriminator;
            anythingChanged = true;
        }

        if (member.AvatarHash != avatarHash)
        {
            member.AvatarHash = avatarHash;
            anythingChanged = true;
        }

        if (anythingChanged)
        {
            await uow.SaveAsync();
        }
    }
}
