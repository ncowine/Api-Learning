namespace Testers.Api.Auth;

public static class AuthSchemes
{
    public const string OktaJwt = "OktaJwt";
    public const string ApiKey = "ApiKey";
    public const string AnyAuthenticated = "AnyAuthenticated";  // either scheme succeeds
}
