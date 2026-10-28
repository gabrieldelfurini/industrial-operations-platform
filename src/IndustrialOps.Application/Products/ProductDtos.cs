using IndustrialOps.Domain.Products;

namespace IndustrialOps.Application.Products;

public sealed record CreateProductRequest(string Code, string Name, string UnitOfMeasure);

public sealed record UpdateProductRequest(string Name, string UnitOfMeasure);

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string UnitOfMeasure,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProductResponse From(Product p) =>
        new(p.Id, p.Code, p.Name, p.UnitOfMeasure, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
