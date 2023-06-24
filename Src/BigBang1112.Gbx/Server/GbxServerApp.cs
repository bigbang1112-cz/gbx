using AspNet.Security.OAuth.Discord;
using BigBang1112.Gbx.Server.Endpoints;
using BigBang1112.Gbx.Server.Extensions;
using BigBang1112.Gbx.Server.Middlewares;
using BigBang1112.Gbx.Server.Options;
using BigBang1112.Gbx.Server.Repos;
using BigBang1112.Gbx.Shared;
using ClipCheckpoint;
using ClipInput;
using GbxToolAPI.Server.Options;
using MapViewerEngine.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using Octokit;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112.Gbx.Server;

internal static class GbxServerApp
{
    internal static void Services(IServiceCollection services, ConfigurationManager config, IWebHostEnvironment env)
    {
        services.AddOptions<DatabaseOptions>().Bind(config.GetSection(Constants.Database));
        services.AddOptions<DiscordOptions>().Bind(config.GetSection(Constants.Discord));
        services.AddOptions<SeqOptions>().Bind(config.GetSection(Constants.Seq));

        services.AddAuthorization(SharedOptions.Authorization);

        if (env.IsDevelopment())
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/signin";

                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    };
                    
                    options.Events.OnRedirectToLogin = async context =>
                    {
                        var autoLogin = config.GetValue<bool>("AutoLogin");

                        if (!autoLogin && context.Request.Path.StartsWithSegments("/api/v1/identity"))
                        {
                            context.Response.StatusCode = 401;
                            return;
                        }

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, Shared.Constants.Developer),
                            new Claim(ClaimTypes.Role, Shared.Constants.SuperAdmin),
                            new Claim(ClaimTypes.Role, Shared.Constants.Developer)
                        };
                        
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await context.HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity), context.Properties);

                        if (autoLogin && context.Request.Path.StartsWithSegments("/api/v1/identity"))
                        {
                            context.Response.Redirect("/api/v1/identity");
                        }
                    };
                });
        }
        else
        {
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
                        if (context.Request.Path.StartsWithSegments(Constants.ApiRoute))
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };

                    options.Events.OnTicketReceived = DiscordAuthentication.TicketReceived;
                });
        }

        services.AddHttpClient(Constants.MxTrack, client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Gbx Web Tools by BigBang1112 (Map.Gbx request)");
        });

        services.AddHttpClient(Constants.MxReplay, client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Gbx Web Tools by BigBang1112 (Replay.Gbx request)");
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
            return new MySqlConnection(config.GetConnectionString(Constants.Gbx));
        });

        services.AddSingleton<IGitHubClient>(sp =>
        {
            var options = config.GetSection(Constants.GitHub).Get<GitHubOptions>() ?? throw new Exception("GitHub options are not configured.");
            
            return new GitHubClient(new ProductHeaderValue("GbxWebTools"))
            {
                Credentials = new Credentials(options.Client.Id, options.Client.Secret)
            };
        });

        services.AddScoped<IMemberRepo, MemberRepo>();
        services.AddScoped<IGbxUnitOfWork, GbxUnitOfWork>();
        
        services.AddDbContext<GbxContext>(options =>
        {
            if (config.GetSection(Constants.Database).Get<DatabaseOptions>()?.InMemory == true)
            {
                options.UseInMemoryDatabase(Constants.Gbx);
            }
            else
            {
                var connectionString = config.GetConnectionString(Constants.Gbx);
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

        services.AddToolServer<MapViewerEngineServer>(config, "MapViewerEngine");
    }

    internal static void Middleware(WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context =>
                {
                    if (context.Request.Path.StartsWithSegments(Constants.ApiRoute))
                    {
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect("/Error");
                    return Task.CompletedTask;
                }
            });
            
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        
        app.UseRouting();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseAuthentication();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "SpecialFiles")),
            RequestPath = "/teaser"
        });

        app.UseAuthorization();

        app.UseMiddleware<GbxApiMiddleware>();
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
