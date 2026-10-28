using IndustrialOps.Domain.Products;

namespace IndustrialOps.Application.Common.Abstractions;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    void Add(Product product);
}
