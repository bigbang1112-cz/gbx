using BigBang1112.Gbx.Server.Endpoints;
using Microsoft.AspNetCore.Routing;
using System.Collections.Immutable;

namespace BigBang1112.Gbx.Server.Extensions;

public static class EndpointServiceExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpoints = typeof(EndpointServiceExtensions).Assembly
            .ExportedTypes
            .Where(x => typeof(IEndpoint).IsAssignableFrom(x) && !x.IsInterface);

        foreach (var endpoint in endpoints)
        {
            services.AddSingleton(typeof(IEndpoint), endpoint);
        }

        return services;
    }

    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IEndpoint>();
        
        foreach (var endpoint in endpoints)
        {
            var type = endpoint.GetType();

            if (type.Namespace is null)
            {
                throw new Exception("Endpoint namespace is null");
            }
            
            if (type.Namespace.Length < Constants.EndpointsNamespace.Length)
            {
                throw new Exception("Invalid namespace for endpoint");
            }

            if (type.Namespace.Length == Constants.EndpointsNamespace.Length)
            {
                endpoint.Endpoint(app);
            }
            else
            {
                var route = type.Namespace.Substring(Constants.EndpointsNamespace.Length + 1).ToLower().Replace('.', '/');

                endpoint.Endpoint(app.MapGroup(route));
            }
        }
        
        return app;
    }
}
