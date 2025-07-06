using System.Reflection;

namespace Famoria.Infrastructure.Persistence;

public class ContainerResolver
{
    /// <summary>
    ///    A dictionary that contains the registered containers.
    /// </summary>
    public required IReadOnlyDictionary<Type, string> RegisteredContainers { get; init; }

    /// <summary>
    ///    A dictionary that contains the registered partition keys.
    ///    Note: The value should be the property name in the code not the partition address in the container.
    /// </summary>
    public required IReadOnlyDictionary<Type, PropertyInfo> RegisteredPartitionKeys { get; init; }

    /// <summary>
    ///     Resolve the name of a container if it exists in the registered containers dictionary.
    /// </summary>
    /// <param name="entityType">The type of an entity to resolve its container</param>
    /// <returns>The type of the container</returns>
    /// <exception cref="InvalidCastException"></exception>
    public string ResolveName(Type entityType)
    {
        var isResolved = RegisteredContainers.TryGetValue(entityType, out var container);

        if (isResolved is false)
        {
            throw new InvalidCastException($"The container for the type {nameof(entityType)} could not be resolved.");
        }

        return container!;
    }

    /// <summary>
    /// Resolves the name of the lease container for the specified entity type.
    /// </summary>
    /// <param name="entityType">The type of the entity for which the lease container name is resolved.</param>
    /// <returns>A string representing the name of the lease container associated with the specified entity type.</returns>
    public string ResolveLease(Type entityType)
    {
        var container = $"{ResolveName(entityType)}-leases";

        return container!;
    }

    /// <summary>
    ///    Resolve the partition key of a container if it exists in the registered partition keys dictionary.
    /// </summary>
    /// <param name="entityType">The type of an entity to resolve its container</param>
    /// <returns>Returns the partition key property info of the entity container</returns>
    /// <exception cref="InvalidCastException"></exception>
    public PropertyInfo ResolvePartitionKey(Type entityType)
    {
        var isResolved = RegisteredPartitionKeys.TryGetValue(entityType, out var propertyInfo);

        if (isResolved is false)
        {
            throw new InvalidCastException($"The partition key for the type {nameof(entityType)} could not be resolved.");
        }

        return propertyInfo!;
    }
}
