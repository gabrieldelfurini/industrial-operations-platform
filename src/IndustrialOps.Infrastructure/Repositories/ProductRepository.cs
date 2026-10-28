using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Domain.Products;
using IndustrialOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOps.Infrastructure.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    // Read-only queries use AsNoTracking: faster, and EF does not keep a copy for change detection.
    public async Task<IReadOnlyList<Product>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await db.Products
            .AsNoTracking()
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

    // Tracked: the caller may change the entity and then call IUnitOfWork.SaveChangesAsync.
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Code == code, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);
}
