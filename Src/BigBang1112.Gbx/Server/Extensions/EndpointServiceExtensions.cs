using BigBang1112.Gbx.Server.Endpoints;
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
            endpoint.Endpoint(app.MapGroup("api"));
        }
        
        return app;
    }
}
