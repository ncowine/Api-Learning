using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;
using Testers.Application.Behaviors;

namespace Testers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scan for IRequestHandler<,> impls in this assembly.
        services.Scan(s => s
            .FromAssemblyOf<IDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Same for FluentValidation validators.
        services.AddValidatorsFromAssemblyContaining<IDispatcher>(includeInternalTypes: true);

        // Pipeline order (outermost first): Logging -> Validation -> UoW -> Performance -> handler.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
