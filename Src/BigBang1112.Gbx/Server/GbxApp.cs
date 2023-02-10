using AspNet.Security.OAuth.Discord;
using BigBang1112.Gbx.Server.Extensions;
using BigBang1112.Gbx.Server.Hubs;
using BigBang1112.Gbx.Server.Middlewares;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Server;

internal static class GbxApp
{
    internal static void Services(IServiceCollection services, ConfigurationManager config)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("InsiderPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });

        services.AddAuthentication(DiscordAuthenticationDefaults.AuthenticationScheme)
            .AddCookie("Cookies")
            .AddDiscord(options =>
            {
                options.ClientId = config.GetValue<string>("Discord:Client:Id") ?? throw new Exception("Discord ClientId is missing!");
                options.ClientSecret = config.GetValue<string>("Discord:Client:Secret") ?? throw new Exception("Discord ClientSecret is missing!");
                options.SignInScheme = "Cookies";
            });

        services.AddSignalR(options =>
        {
            
        });

        services.AddEndpoints();
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    }
    
    internal static void Middleware(WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<InsiderAuthorizationMiddleware>();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapHub<SecureHub>("/securehub");

        app.UseEndpoints();
        app.UseRouting();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }
}
