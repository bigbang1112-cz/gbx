using BigBang1112.Gbx.Client;
using BigBang1112.Gbx.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

GBX.NET.Lzo.SetLzo(typeof(GBX.NET.LZO.MiniLZO));

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ISecureHubService, SecureHubService>();
builder.Services.AddScoped<HttpClient>(sp => new() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
