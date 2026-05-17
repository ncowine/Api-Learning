using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Testers.Api.Auth;
using Testers.Api.Endpoints;
using Testers.Api.Errors;
using Testers.Application.Abstractions;

namespace Testers.Api;

/// <summary>
/// Wires the API/HTTP edge: auth schemes (Okta JWT + API key), authorization policies, the
/// exception-to-ProblemDetails handler, Swagger with auth, the endpoint scanner, and the
/// HTTP-aware <see cref="ICurrentUser"/> (which overrides the system fallback from Infrastructure).
/// Composed into <c>Program.cs</c> as one call: <c>services.AddApiServices(builder.Configuration)</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ---- HTTP-aware ICurrentUser supersedes Infrastructure's SystemCurrentUser ----
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        // ---- Authentication: Okta JWT + ApiKey ----
        services.Configure<OktaJwtOptions>(configuration.GetSection(OktaJwtOptions.SectionName));

        services.AddAuthentication(AuthSchemes.OktaJwt)
            .AddJwtBearer(AuthSchemes.OktaJwt, opts =>
            {
                var oktaSection = configuration.GetSection(OktaJwtOptions.SectionName);
                opts.Authority = oktaSection["Authority"];
                opts.Audience = oktaSection["Audience"];
                opts.RequireHttpsMetadata = oktaSection.GetValue("RequireHttpsMetadata", true);
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                AuthSchemes.ApiKey, _ => { });

        // ---- Authorization policies ----
        services.AddAuthorization(opts => opts.AddTestersPolicies());

        // ---- Exception handler (RFC 7807 ProblemDetails) ----
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        // ---- Swagger / OpenAPI with auth schemes ----
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Testers API", Version = "v1" });

            c.AddSecurityDefinition(AuthSchemes.OktaJwt, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Okta-issued JWT. Paste the token (no 'Bearer ' prefix).",
            });

            c.AddSecurityDefinition(AuthSchemes.ApiKey, new OpenApiSecurityScheme
            {
                Name = "X-Api-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Service-account API key.",
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = AuthSchemes.OktaJwt, Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>(),
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = AuthSchemes.ApiKey, Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>(),
            });
        });

        // ---- Health checks ----
        services.AddHealthChecks();

        // ---- Endpoint discovery (scans Application assembly for IEndpoint impls) ----
        services.AddEndpointScanning();

        return services;
    }
}
