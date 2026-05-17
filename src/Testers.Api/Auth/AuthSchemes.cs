namespace Testers.Api.Auth;

/// <summary>
/// Authentication scheme names referenced in DI registration, authorization policies, and
/// <c>[Authorize(AuthenticationSchemes = ...)]</c>. Keeping them in one place avoids string
/// drift between the registration site and the consumers.
/// </summary>
public static class AuthSchemes
{
    /// <summary>Okta JWT bearer scheme. Issuer + audience configured from "Okta" config section.</summary>
    public const string OktaJwt = "OktaJwt";

    /// <summary>Custom API-key scheme. Reads <c>X-Api-Key</c> header, looks up hash in AppDb.</summary>
    public const string ApiKey = "ApiKey";

    /// <summary>Combined default policy: a request is authenticated if EITHER scheme succeeds.</summary>
    public const string AnyAuthenticated = "AnyAuthenticated";
}
