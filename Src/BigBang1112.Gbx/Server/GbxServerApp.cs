using AspNet.Security.OAuth.Discord;
using BigBang1112.Gbx.Server.Endpoints;
using BigBang1112.Gbx.Server.Extensions;
using BigBang1112.Gbx.Server.Middlewares;
using BigBang1112.Gbx.Server.Repos;
using BigBang1112.Gbx.Shared;
using ClipCheckpoint;
using ClipInput;
using GbxToolAPI.Server.Options;
using MapViewerEngine.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Server;

internal static class GbxServerApp
{
    internal static void Services(IServiceCollection services, ConfigurationManager config)
    {
        services.AddOptions<DatabaseOptions>().Bind(config.GetSection(Constants.Database));
        services.AddOptions<DiscordOptions>().Bind(config.GetSection(Constants.Discord));
        
        services.AddAuthorization(SharedOptions.Authorization);

        services.AddAuthentication(DiscordAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            })
            .AddDiscord(options =>
            {
                var discordOptions = config.GetSection(Constants.Discord).Get<DiscordOptions>() ?? new DiscordOptions();

                options.ClientId = discordOptions.Client.Id;
                options.ClientSecret = discordOptions.Client.Secret;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnTicketReceived = DiscordAuthentication.TicketReceived;
            });

        services.AddSignalR(options =>
        {
            options.SupportedProtocols = new List<string> { Constants.Messagepack };
        })
            .AddMessagePackProtocol();

        services.AddEndpoints();
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddScoped<IDbConnection>(s =>
        {
            return new MySqlConnection(config.GetConnectionString("Gbx"));
        });

        services.AddScoped<IMemberRepo, MemberRepo>();
        services.AddScoped<IGbxUnitOfWork, GbxUnitOfWork>();
        
        services.AddDbContext<GbxContext>(options =>
        {
            if (config.GetSection(Constants.Database).Get<DatabaseOptions>()?.InMemory == true)
            {
                options.UseInMemoryDatabase("Gbx");
            }
            else
            {
                var connectionString = config.GetConnectionString("Gbx");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        });

        // migrate
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GbxContext>().Database;

            if (db.IsRelational())
            {
                db.Migrate();
            }
        }

        if (config.GetValue<bool>("InsiderMode") == false)
        {
            services.AddToolServer<MapViewerEngineServer>(config, "MapViewerEngine");
        }
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
        
        app.UseRouting();
        
        app.UseResponseCompression();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseAuthentication();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "SpecialFiles")),
            RequestPath = "/teaser"
        });

        app.UseAuthorization();
        
        app.UseMiddleware<InsiderAuthorizationMiddleware>();
        app.UseMiddleware<RegularAuthorizationMiddleware>();
        
        app.UseEndpoints();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });

        app.UseToolAssets<ClipInputTool>();
        app.UseToolAssets<ClipCheckpointTool>();

        app.UseToolServer<MapViewerEngineServer>();
        
        app.UseToolEndpoints();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }
}
