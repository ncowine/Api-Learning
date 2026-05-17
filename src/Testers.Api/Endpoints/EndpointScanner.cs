using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Testers.Application;
using Testers.SharedKernel.Abstractions;

namespace Testers.Api.Endpoints;

// Scans the Application assembly for IEndpoint impls, registers them singleton, then maps
// each under /api/v1. Adding a slice = drop a file + run.
public static class EndpointScanner
{
    public static IServiceCollection AddEndpointScanning(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationAssemblyMarker>()
            .AddClasses(c => c.AssignableTo<IEndpoint>())
            .As<IEndpoint>()
            .WithSingletonLifetime());
        return services;
    }

    public static IEndpointRouteBuilder MapDiscoveredEndpoints(this WebApplication app, string routePrefix = "/api/v1")
    {
        var group = app.MapGroup(routePrefix);
        foreach (var endpoint in app.Services.GetServices<IEndpoint>())
            endpoint.MapEndpoint(group);
        return app;
    }
}
