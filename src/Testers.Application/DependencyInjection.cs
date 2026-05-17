using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Testers.SharedKernel.Abstractions;
using Testers.SharedKernel.Behaviors;

namespace Testers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scan for IRequestHandler<,> impls in this assembly. ApplicationAssemblyMarker is a
        // local type whose only job is to identify the Application assembly to the scanner.
        services.Scan(s => s
            .FromAssemblyOf<ApplicationAssemblyMarker>()
            .AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Same for FluentValidation validators.
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>(includeInternalTypes: true);

        // Pipeline order (outermost first): Logging -> Validation -> UoW -> Performance -> handler.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
