using System.Reflection;

namespace Famoria.Infrastructure.Persistence;

internal class ContainerResolver
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
