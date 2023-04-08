using GbxToolAPI.Server;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BigBang1112.Gbx.Server.Extensions;

internal static partial class ToolEndpointServiceExtensions
{
    [GeneratedRegex(@"Server\.Endpoints\.V([0-9]+)\.?(.*)")]
    private static partial Regex RegexEndpointRoute();

    public static IServiceCollection AddToolEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.ExportedTypes
            .Where(x => typeof(IToolEndpoint).IsAssignableFrom(x) && !x.IsInterface);

        foreach (var endpoint in endpoints)
        {
            services.AddSingleton(typeof(IToolEndpoint), endpoint);
        }

        return services;
    }

    public static IEndpointRouteBuilder UseToolEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IToolEndpoint>();

        foreach (var endpoint in endpoints)
        {
            var type = endpoint.GetType();

            if (type.Namespace is null)
            {
                throw new Exception("Endpoint namespace is null");
            }

            var match = RegexEndpointRoute().Match(type.Namespace);

            if (!match.Success)
            {
                throw new Exception("Endpoint namespace is invalid");
            }

            var toolRoute = type.Assembly.GetCustomAttribute<ToolEndpointAttribute>()?.Route ?? throw new Exception("Tool assembly is missing ToolEndpointAttribute");

            var route = $"api/v{match.Groups[1].Value}/tools/{toolRoute}/{match.Groups[2].Value.Replace('.', '/').ToLower()}";

            endpoint.Endpoint(app.MapGroup(route));
        }
        
        return app;
    }
}
