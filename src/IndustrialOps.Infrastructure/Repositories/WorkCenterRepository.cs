using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Domain.WorkCenters;
using IndustrialOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOps.Infrastructure.Repositories;

internal sealed class WorkCenterRepository(AppDbContext db) : IWorkCenterRepository
{
    public async Task<IReadOnlyList<WorkCenter>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await db.WorkCenters
            .AsNoTracking()
            .Where(w => includeInactive || w.IsActive)
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);

    public Task<WorkCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.WorkCenters.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.WorkCenters.AnyAsync(w => w.Code == code, cancellationToken);

    public void Add(WorkCenter workCenter) => db.WorkCenters.Add(workCenter);
}
