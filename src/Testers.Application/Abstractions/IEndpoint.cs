using Microsoft.AspNetCore.Routing;

namespace Testers.Application.Abstractions;

// Slice endpoint classes implement this. Api's EndpointScanner finds + maps them at startup.
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
