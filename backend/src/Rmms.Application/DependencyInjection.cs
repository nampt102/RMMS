using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Rmms.Application.Common.Abstractions;
using Rmms.Application.Common.Behaviors;
using Rmms.Application.Maintenance;

namespace Rmms.Application;

/// <summary>
/// Registers Application-layer services: Mediator, validators, Mapster, pipeline behaviors.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Mediator (source-generated) — registers handlers from this assembly.
        services.AddMediator(opts =>
        {
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });

        // FluentValidation — pick up all AbstractValidator<T> in this assembly.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Pipeline behaviors (order matters: outermost listed first).
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Maintenance jobs (invoked by the Hangfire worker).
        services.AddScoped<ITokenCleanupService, TokenCleanupService>();
        services.AddScoped<IAttendancePhotoRetentionService, AttendancePhotoRetentionService>();

        return services;
    }
}
