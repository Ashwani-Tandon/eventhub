// Provides a shared identity property for future domain entities.
// It contains no persistence or HTTP dependencies, preserving the Domain layer boundary.
namespace EventHub.BuildingBlocks.Domain;

/// <summary>
/// Provides a shared identity property for future domain entities. It contains no persistence or HTTP dependencies, preserving the Domain layer boundary.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Stores the aggregate identity without introducing persistence or HTTP dependencies.
    /// </summary>
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; protected init; }
}
