using BigBang1112.Gbx.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BigBang1112.Gbx.Client;

public static class GbxClientApp
{
    internal static void Services(IServiceCollection services, IWebAssemblyHostEnvironment host)
    {
        services.AddScoped<ISecureHubService, SecureHubService>();
        services.AddScoped<HttpClient>(sp => new() { BaseAddress = new Uri(host.BaseAddress) });

        services.AddScoped<ToolManager>();

        ToolManager.Services(services);
    }
}
