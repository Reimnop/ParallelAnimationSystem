using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace ParallelAnimationSystem.Util;

/// <summary>
/// Represents a service definition for registering services in a dependency injection container.
/// </summary>
/// <typeparam name="T">The type of the service being defined.</typeparam>
public class ServiceDefinition<T>
{
    /// <summary>
    /// Gets or sets the implementation type for the service.
    /// This property is used when registering the service using a type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public required Type? ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the implementation instance for the service.
    /// This property is used when registering the service using an existing instance.
    /// </summary>
    public required T? Implementation { get; set; }

    /// <summary>
    /// Gets or sets the factory function for creating the service.
    /// This property is used when registering the service using a factory method.
    /// </summary>
    public required Func<IServiceProvider, T>? Factory { get; set; }
    
    /// <summary>
    /// Registers the service to the provided IServiceCollection with the specified lifetime.
    /// </summary>
    /// <param name="services">The IServiceCollection to which the service will be added.</param>
    /// <param name="serviceLifetime">The lifetime of the service in the dependency injection container.</param>
    /// <exception cref="InvalidOperationException">Thrown when no implementation, type, or factory is provided, or the lifetime is invalid.</exception>
    public void RegisterToServiceCollection(IServiceCollection services, ServiceLifetime serviceLifetime)
    {
        ServiceDescriptor serviceDescriptor;
        if (ImplementationType is not null)
        {
            serviceDescriptor = new ServiceDescriptor(typeof(T), ImplementationType, serviceLifetime);
        }
        else if (Implementation is not null)
        {
            serviceDescriptor = new ServiceDescriptor(typeof(T), Implementation);
        }
        else if (Factory is not null)
        {
            serviceDescriptor = new ServiceDescriptor(typeof(T), serviceProvider => Factory(serviceProvider)!, serviceLifetime);
        }
        else
        {
            throw new InvalidOperationException("No implementation or factory was provided.");
        }
        
        services.Add(serviceDescriptor);
    }

    /// <summary>
    /// Implicitly converts a type to a <see cref="ServiceDefinition{T}"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>A <see cref="ServiceDefinition{T}"/> with the specified type.</returns>
    public static implicit operator ServiceDefinition<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        => new()
        {
            ImplementationType = type,
            Implementation = default,
            Factory = null,
        };
    
    /// <summary>
    /// Implicitly converts an implementation to a <see cref="ServiceDefinition{T}"/>.
    /// </summary>
    /// <param name="implementation">The implementation to convert.</param>
    /// <returns>A <see cref="ServiceDefinition{T}"/> with the specified implementation.</returns>
    public static implicit operator ServiceDefinition<T>(T implementation)
        => new()
        {
            ImplementationType = null,
            Implementation = implementation,
            Factory = null,
        };
    
    /// <summary>
    /// Implicitly converts a factory function to a <see cref="ServiceDefinition{T}"/>.
    /// </summary>
    /// <param name="factory">The factory to convert.</param>
    /// <returns>A <see cref="ServiceDefinition{T}"/> with the specified factory.</returns>
    public static implicit operator ServiceDefinition<T>(Func<IServiceProvider, T> factory)
        => new()
        {
            ImplementationType = null,
            Implementation = default,
            Factory = factory,
        };
}