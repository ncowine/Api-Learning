using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Testers.Application.Abstractions;

namespace Testers.Api.Auth;

public static class AuthorizationPolicies
{
    public const string AnyAuthenticated = AuthSchemes.AnyAuthenticated;
    public const string HumanOnly = "HumanOnly";
    public const string ServiceOnly = "ServiceOnly";

    public static AuthorizationOptions AddTestersPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AnyAuthenticated, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey));

        options.AddPolicy(HumanOnly, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey)
            .RequireAssertion(ctx => Kind(ctx.User) is null or nameof(UserKind.Human)));

        options.AddPolicy(ServiceOnly, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey)
            .RequireAssertion(ctx => Kind(ctx.User) == nameof(UserKind.Service)));

        // RequireAuthorization() (no name) uses this.
        options.DefaultPolicy = options.GetPolicy(AnyAuthenticated)!;

        return options;
    }

    private static string? Kind(ClaimsPrincipal user) =>
        user.FindFirst(ApiKeyAuthenticationHandler.ClaimTypeKind)?.Value;
}
