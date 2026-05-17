using Microsoft.AspNetCore.Routing;

namespace Testers.Application.Abstractions;

/// <summary>
/// Implemented by an endpoint class in a feature slice. The Api project's endpoint scanner
/// discovers every <see cref="IEndpoint"/> implementation at startup and calls
/// <see cref="MapEndpoint"/> on each, passing a route group builder shared by all slices.
///
/// Each slice's endpoint class lives in the slice folder next to its Command / Handler /
/// Validator / Result. Adding a new feature requires no central registration — the scanner
/// finds it automatically.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
