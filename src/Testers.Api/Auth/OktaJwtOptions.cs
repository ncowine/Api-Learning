namespace Testers.Api.Auth;

/// <summary>
/// Bound from the <c>Okta</c> section of <c>appsettings.json</c>.
///
/// Sample:
/// <code>
/// "Okta": {
///   "Authority":         "https://your-tenant.okta.com/oauth2/default",
///   "Audience":          "api://default",
///   "RequireHttpsMetadata": true
/// }
/// </code>
/// </summary>
public sealed class OktaJwtOptions
{
    public const string SectionName = "Okta";

    /// <summary>Okta authorisation server issuer URL (the <c>iss</c> claim consumers must match).</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>Expected <c>aud</c> claim — the API's identifier as registered in Okta.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Set to false only for non-prod against Okta's local dev server (preview/dev tenants).</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
