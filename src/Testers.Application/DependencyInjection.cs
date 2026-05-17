using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;
using Testers.Application.Behaviors;

namespace Testers.Application;

/// <summary>
/// Registers everything in <c>Testers.Application</c> with DI: feature handlers, FluentValidation
/// validators (both scanned from this assembly), and the four pipeline behaviors in the order
/// they wrap the handler — outermost first.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scan this assembly for every IRequestHandler<,> implementation and register it scoped.
        // New feature slices need no DI registration boilerplate — drop a handler into a folder
        // under Features/ and it's wired automatically.
        services.Scan(scan => scan
            .FromAssemblyOf<IDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Same idea for FluentValidation: classes implementing AbstractValidator<TRequest> auto-register.
        services.AddValidatorsFromAssemblyContaining<IDispatcher>(includeInternalTypes: true);

        // Pipeline behaviors in execution order (outermost first):
        //   Logging -> Validation -> UnitOfWork -> Performance -> Handler
        //
        // Logging is outermost so it captures everything (including validation failures + tx times).
        // UnitOfWork wraps the handler so domain writes are transactional, but skips for queries.
        // Performance is innermost so its timing reflects pure handler work, not the pipeline.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
