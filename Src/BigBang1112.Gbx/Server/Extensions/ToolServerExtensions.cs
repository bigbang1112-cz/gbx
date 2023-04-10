using BigBang1112.Gbx.Server.Endpoints;
using GbxToolAPI.Server;
using GbxToolAPI.Server.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Reflection;

namespace BigBang1112.Gbx.Server.Extensions;

internal static class ToolServerExtensions
{
    public static void AddToolServer<T>(this IServiceCollection services, IConfiguration config, string dbName) where T : class, IServer, new()
    {
        services.AddSingleton<T>();

        T.Services(services);

        var assembly = typeof(T).Assembly;

        services.AddToolEndpoints(assembly);

        foreach (var type in assembly.DefinedTypes)
        {
            TryAddToolDbContext(services, config, dbName, type);

            services.AddScoped<ISqlConnection<T>, SqlConnection<T>>(provider =>
            {
                return new(new MySqlConnection(config.GetConnectionString(provider.GetRequiredService<T>().ConnectionString)));
            });
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
            })).FirstOrDefault() ?? throw new Exception("AddDbContext method not found!");

        var genericMethod = methodInfo.MakeGenericMethod(type);

        genericMethod.Invoke(null, new object[]
        {
            services,
            delegate (DbContextOptionsBuilder options) {
                var dbName = "bigbang1112_gbx_tool_" + databaseName.ToLower();

                if (config.GetSection(Constants.Database).Get<DatabaseOptions>()?.InMemory == true)
                {
                    options.UseInMemoryDatabase(dbName);
                }
                else
                {
                    var connectionString = config.GetConnectionString(databaseName);

                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), options =>
                    {
                        options.MigrationsAssembly(type.Assembly.GetName().Name);
                    });
                }
            },
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped
        });

        using var scope = services.BuildServiceProvider().CreateScope();

        var db = ((DbContext)scope.ServiceProvider.GetRequiredService(type)).Database;

        if (db.IsRelational())
        {
            db.Migrate();
        }

        return true;
    }

    public static void UseToolServer<T>(this IEndpointRouteBuilder app) where T : IServer
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
