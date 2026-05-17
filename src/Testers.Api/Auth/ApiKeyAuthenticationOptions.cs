using Microsoft.AspNetCore.Authentication;

namespace Testers.Api.Auth;

/// <summary>
/// Options for the custom <c>ApiKey</c> authentication scheme. Header name is configurable
/// (default <c>X-Api-Key</c>) so tests / non-standard callers can override.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
}
