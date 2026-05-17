using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Testers.Application.Abstractions;

namespace Testers.Api.Auth;

/// <summary>
/// Named authorization policies referenced by endpoint <c>.RequireAuthorization("...")</c> calls.
/// Centralised here so policy semantics don't drift across endpoints.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Default policy: any successfully authenticated principal from either scheme.</summary>
    public const string AnyAuthenticated = AuthSchemes.AnyAuthenticated;

    /// <summary>Only humans (Okta JWT). Service accounts get 403.</summary>
    public const string HumanOnly = "HumanOnly";

    /// <summary>Only service accounts (API key). Humans get 403.</summary>
    public const string ServiceOnly = "ServiceOnly";

    public static AuthorizationOptions AddTestersPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AnyAuthenticated, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey));

        options.AddPolicy(HumanOnly, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey)
            .RequireAssertion(ctx => KindClaim(ctx.User) is null or nameof(UserKind.Human)));

        options.AddPolicy(ServiceOnly, p => p
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(AuthSchemes.OktaJwt, AuthSchemes.ApiKey)
            .RequireAssertion(ctx => KindClaim(ctx.User) == nameof(UserKind.Service)));

        // Default policy (used by RequireAuthorization() without a name): any authenticated.
        options.DefaultPolicy = options.GetPolicy(AnyAuthenticated)!;

        return options;
    }

    private static string? KindClaim(ClaimsPrincipal user) =>
        user.FindFirst(ApiKeyAuthenticationHandler.ClaimTypeKind)?.Value;
}
