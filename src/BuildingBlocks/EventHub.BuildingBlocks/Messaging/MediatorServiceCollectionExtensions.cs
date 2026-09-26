using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventHub.BuildingBlocks.Messaging;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly must be supplied.",
                nameof(assemblies));
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ISender, Sender>();

        foreach (var implementationType in assemblies
                     .Distinct()
                     .SelectMany(static assembly => assembly.DefinedTypes)
                     .Where(static type => type is { IsAbstract: false, IsInterface: false }))
        {
            RegisterHandlers(
                services,
                implementationType);

            RegisterClosedInterfaces(
                services,
                implementationType,
                typeof(IValidator<>));
        }

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        TypeInfo implementationType)
    {
        // Reflection is limited to startup registration. Each adapter preserves the
        // request/response generic types needed to resolve a strongly typed handler.
        foreach (var serviceType in implementationType.ImplementedInterfaces
                     .Where(static type =>
                         type.IsGenericType
                         && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
        {
            services.AddTransient(serviceType, implementationType);

            var dispatcherType = typeof(RequestDispatcher<,>)
                .MakeGenericType(serviceType.GenericTypeArguments);
            var dispatcher = Activator.CreateInstance(dispatcherType)
                ?? throw new InvalidOperationException(
                    $"Could not register a dispatcher for {serviceType.GenericTypeArguments[0].Name}.");

            services.AddSingleton(typeof(IRequestDispatcher), dispatcher);
        }
    }

    private static void RegisterClosedInterfaces(
        IServiceCollection services,
        TypeInfo implementationType,
        Type openGenericType)
    {
        foreach (var serviceType in implementationType.ImplementedInterfaces
                     .Where(type =>
                         type.IsGenericType
                         && type.GetGenericTypeDefinition() == openGenericType))
        {
            services.AddTransient(serviceType, implementationType);
        }
    }
}
// Registers handlers, validators, and typed dispatch adapters by scanning supplied assemblies at startup.
// Also registers the behaviors in their execution order so each service shares the same pipeline.
