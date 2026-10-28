using IndustrialOps.Domain.Common;

namespace IndustrialOps.Domain.Products;

/// <summary>A material that can be manufactured (e.g. "PRD-001 - Steel Bracket").</summary>
public sealed class Product : Entity
{
    // Required by EF Core to materialize the entity from the database.
    private Product()
    {
    }

    public Product(string code, string name, string unitOfMeasure, DateTime utcNow)
        : base(utcNow)
    {
        Code = Guard.NotEmpty(code, nameof(Code)).ToUpperInvariant();
        Name = Guard.NotEmpty(name, nameof(Name));
        UnitOfMeasure = Guard.NotEmpty(unitOfMeasure, nameof(UnitOfMeasure)).ToUpperInvariant();
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string UnitOfMeasure { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public void Update(string name, string unitOfMeasure, DateTime utcNow)
    {
        Name = Guard.NotEmpty(name, nameof(Name));
        UnitOfMeasure = Guard.NotEmpty(unitOfMeasure, nameof(UnitOfMeasure)).ToUpperInvariant();
        Touch(utcNow);
    }

    /// <summary>Soft delete: keeps history of production orders that reference it.</summary>
    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        Touch(utcNow);
    }
}
