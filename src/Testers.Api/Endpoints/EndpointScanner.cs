using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;

namespace Testers.Api.Endpoints;

/// <summary>
/// Auto-registers + maps every <see cref="IEndpoint"/> implementation in the Application assembly.
/// Feature slices don't have to touch Program.cs — drop a class implementing
/// <see cref="IEndpoint"/> next to a handler and it's reachable at startup.
/// </summary>
public static class EndpointScanner
{
    public static IServiceCollection AddEndpointScanning(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<IDispatcher>()
            .AddClasses(c => c.AssignableTo<IEndpoint>())
            .As<IEndpoint>()
            .WithSingletonLifetime());
        return services;
    }

    /// <summary>Maps all registered <see cref="IEndpoint"/> contributors under the given route group.</summary>
    public static IEndpointRouteBuilder MapDiscoveredEndpoints(this WebApplication app, string routePrefix = "/api/v1")
    {
        var group = app.MapGroup(routePrefix);
        var endpoints = app.Services.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(group);
        }

        return app;
    }
}
