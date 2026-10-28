namespace IndustrialOps.Domain.Common;

/// <summary>
/// Base class for all entities. Ids are generated in the application (UUID v7),
/// so an entity is valid and identifiable before it is ever persisted.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Entity()
    {
    }

    protected Entity(DateTime utcNow)
    {
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    protected void Touch(DateTime utcNow) => UpdatedAt = utcNow;
}
