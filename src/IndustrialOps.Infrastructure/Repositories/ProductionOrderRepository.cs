using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Domain.ProductionOrders;
using IndustrialOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOps.Infrastructure.Repositories;

internal sealed class ProductionOrderRepository(AppDbContext db) : IProductionOrderRepository
{
    public async Task<IReadOnlyList<ProductionOrder>> ListAsync(ProductionOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.Product)
            .Include(o => o.WorkCenter)
            .AsQueryable();

        if (status is not null)
            query = query.Where(o => o.Status == status);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ProductionOrders
            .Include(o => o.Product)
            .Include(o => o.WorkCenter)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductionReport>> ListReportsAsync(Guid productionOrderId, CancellationToken cancellationToken = default) =>
        await db.ProductionReports
            .AsNoTracking()
            .Where(r => r.ProductionOrderId == productionOrderId)
            .OrderBy(r => r.ReportedAt)
            .ToListAsync(cancellationToken);

    // Compile-time constant: no user input ever reaches this SQL.
    private const string NextOrderNumberSql = $"SELECT nextval('{AppDbContext.OrderNumberSequence}') AS \"Value\"";

    public Task<long> GetNextOrderSequenceAsync(CancellationToken cancellationToken = default) =>
        // A sequence is concurrency-safe: two simultaneous requests never get the same number.
        db.Database
            .SqlQueryRaw<long>(NextOrderNumberSql)
            .SingleAsync(cancellationToken);

    public void Add(ProductionOrder order) => db.ProductionOrders.Add(order);
}
