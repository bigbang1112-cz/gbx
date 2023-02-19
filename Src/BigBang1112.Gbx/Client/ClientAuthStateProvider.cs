using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BigBang1112.Gbx.Client;

public class ClientAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _http;

    public ClientAuthStateProvider(HttpClient http)
    {
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var response = await _http.GetAsync("/api/v1/identity");
        
        var identity = await GetIdentityAsync(response);

        return new AuthenticationState(identity is null ? new ClaimsPrincipal() : new ClaimsPrincipal(identity));
    }

    private async Task<ClaimsIdentity?> GetIdentityAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // parse the ClaimsIdentity from the API response
        var claimStrings = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();

        if (claimStrings is null)
        {
            return null;
        }

        return new ClaimsIdentity(ClaimStringsToClaims(claimStrings), "Discord");
    }

    private static IEnumerable<Claim> ClaimStringsToClaims(IDictionary<string, List<string>> claimStrings)
    {
        foreach (var (type, values) in claimStrings)
        {
            foreach (var value in values)
            {
                yield return new Claim(type, value);
            }
        }
    }
}
