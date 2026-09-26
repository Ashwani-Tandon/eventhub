namespace EventHub.BuildingBlocks.Domain;

public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; protected init; }
}
// Provides a shared identity property for future domain entities.
// It contains no persistence or HTTP dependencies, preserving the Domain layer boundary.
