using BigBang1112.Gbx.Shared;
using GbxToolAPI.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace BigBang1112.Gbx.Client;

public class ClientAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _http;
    private readonly SettingsService _settings;

    public ClientAuthStateProvider(HttpClient http, SettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = await GetIdentityAsync();

        return new AuthenticationState(identity is null ? new ClaimsPrincipal() : new ClaimsPrincipal(identity));
    }

    private async Task<ClaimsIdentity?> GetIdentityAsync()
    {
        var response = await _http.GetAsync("/api/v1/identity");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try // Hacky way to avoid the redirect issue
        {
            // parse the ClaimsIdentity from the API response
            var identityModel = await response.Content.ReadFromJsonAsync<Identity>();

            if (identityModel is null)
            {
                return null;
            }

            //_settings.AutoLogin = identityModel.AutoLogin;

            return new ClaimsIdentity(ClaimStringsToClaims(identityModel.Claims), identityModel.AuthenticationType);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static IEnumerable<Claim> ClaimStringsToClaims(IDictionary<string, List<string>> claimStrings)
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
