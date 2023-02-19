using AspNet.Security.OAuth.Discord;
using BigBang1112.Gbx.Server.Extensions;
using BigBang1112.Gbx.Server.Middlewares;
using BigBang1112.Gbx.Shared;
using GbxToolAPI.Server;
using GbxToolAPI.Server.Options;
using MapViewerEngine.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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
            .AddCookie(Constants.Cookies)
            .AddDiscord(options =>
            {
                var discordOptions = config.GetSection(Constants.Discord).Get<DiscordOptions>() ?? new DiscordOptions();

                options.ClientId = discordOptions.Client.Id;
                options.ClientSecret = discordOptions.Client.Secret;
                options.SignInScheme = Constants.Cookies;
            });

        services.AddSignalR(options =>
        {
            options.SupportedProtocols = new List<string> { Constants.Messagepack };
        })
            .AddMessagePackProtocol();

        services.AddEndpoints();
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        AddToolServer<MapViewerEngineServer>(services, config, "MapViewerEngine");
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

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<InsiderAuthorizationMiddleware>();
        app.UseMiddleware<RegularAuthorizationMiddleware>();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        MapToolServer<MapViewerEngineServer>(app);

        app.UseEndpoints();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }

    private static void AddToolServer<T>(IServiceCollection services, IConfiguration config, string dbName) where T : IServer, new()
    {
        var server = new T();
        server.Services(services);

        foreach (var type in typeof(T).Assembly.DefinedTypes)
        {
            TryAddToolDbContext(services, config, dbName, type);
        }
    }

    private static bool TryAddToolDbContext(IServiceCollection services, IConfiguration config, string databaseName, TypeInfo type)
    {
        if (!type.IsSubclassOf(typeof(DbContext)))
        {
            return false;
        }

        // ugly path to AddDbContext<type>

        var methodInfo = typeof(EntityFrameworkServiceCollectionExtensions).GetMethods()
            .Where(x => x.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                && x.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[]
            {
                typeof(IServiceCollection),
                typeof(Action<DbContextOptionsBuilder>),
                typeof(ServiceLifetime),
                typeof(ServiceLifetime)
            })).FirstOrDefault();

        if (methodInfo is null)
        {
            throw new Exception("AddDbContext method not found!");
        }

        var genericMethod = methodInfo.MakeGenericMethod(type);

        genericMethod.Invoke(null, new object[]
        {
            services,
            delegate (DbContextOptionsBuilder options) {
                var dbName = "bigbang1112_gbx_tool_" + databaseName.ToLower();

                if (config.GetValue<bool>("Database:InMemory"))
                {
                    options.UseInMemoryDatabase(dbName);
                }
                else
                {
                    var connectionString = config.GetConnectionString(dbName);

                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                }
            },
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped
        });

        return true;
    }

    private static void MapToolServer<T>(IEndpointRouteBuilder app) where T : IServer
    {
        foreach (var type in typeof(T).Assembly.DefinedTypes)
        {
            TryMapToolHub(app, type);
        }
    }

    private static bool TryMapToolHub(IEndpointRouteBuilder app, TypeInfo type)
    {
        if (!type.IsSubclassOf(typeof(Hub)))
        {
            return false;
        }
        
        // ugly path to MapHub<type>

        var methodInfo = typeof(HubEndpointRouteBuilderExtensions).GetMethod(nameof(HubEndpointRouteBuilderExtensions.MapHub), new[] { typeof(IEndpointRouteBuilder), typeof(string) });

        if (methodInfo is null)
        {
            throw new Exception("MapHub method not found!");
        }

        methodInfo.MakeGenericMethod(type).Invoke(null, new object[] { app, "/" + type.Name.ToLower() });

        return true;
    }
}
