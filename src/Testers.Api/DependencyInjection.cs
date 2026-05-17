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

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // HTTP-aware ICurrentUser - supersedes SystemCurrentUser from Infrastructure.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        // Auth: Okta JWT + ApiKey schemes.
        services.Configure<OktaJwtOptions>(configuration.GetSection(OktaJwtOptions.SectionName));

        services.AddAuthentication(AuthSchemes.OktaJwt)
            .AddJwtBearer(AuthSchemes.OktaJwt, opts =>
            {
                var okta = configuration.GetSection(OktaJwtOptions.SectionName);
                opts.Authority = okta["Authority"];
                opts.Audience = okta["Audience"];
                opts.RequireHttpsMetadata = okta.GetValue("RequireHttpsMetadata", true);
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, _ => { });

        services.AddAuthorization(opts => opts.AddTestersPolicies());

        // RFC 7807 ProblemDetails on exceptions.
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        // Swagger with both auth schemes.
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
                Description = "Okta JWT (no 'Bearer ' prefix).",
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

        services.AddHealthChecks();
        services.AddEndpointScanning();

        return services;
    }
}
