using BigBang1112.Gbx.Client.Services;
using BigBang1112.Gbx.Shared;
using Blazored.LocalStorage;
using GbxToolAPI;
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
        services.AddBlazoredLocalStorage(options =>
        {
            foreach (var conv in Json.DefaultOptions.Converters)
            {
                options.JsonSerializerOptions.Converters.Add(conv);
            }
        });

        services.AddScoped<IToolManager, ToolManager>(); // registers the tool manager so that it can implement some testability
        services.AddScoped<IWorkflowManager, WorkflowManager>();
        services.AddScoped<SettingsService>();
        services.AddScoped<IGbxService, GbxService>();
        services.AddScoped<IPropertyValueDisplayService, PropertyValueDisplayService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<ILogger, Logger>();

        ToolManager.Services(services); // registers the individual "strong" tools
    }
}
