namespace Testers.Api.Auth;

public sealed class OktaJwtOptions
{
    public const string SectionName = "Okta";

    public string Authority { get; init; } = string.Empty;          // iss claim
    public string Audience { get; init; } = string.Empty;           // aud claim
    public bool RequireHttpsMetadata { get; init; } = true;         // false only for dev tenants
}
