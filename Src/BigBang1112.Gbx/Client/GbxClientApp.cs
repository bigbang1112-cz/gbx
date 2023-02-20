﻿using BigBang1112.Gbx.Client.Services;
using BigBang1112.Gbx.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BigBang1112.Gbx.Client;

public static class GbxClientApp
{
    internal static void Services(IServiceCollection services, IWebAssemblyHostEnvironment host)
    {
        services.AddScoped<HttpClient>(sp => new() { BaseAddress = new Uri(host.BaseAddress) });
        
        services.AddScoped<AuthenticationStateProvider, ClientAuthStateProvider>();
        services.AddAuthorizationCore(SharedOptions.Authorization);

        services.AddScoped<ToolManager>(); // registers the tool manager so that it can implement some testability

        ToolManager.Services(services); // registers the individual "strong" tools
    }
}