using IndustrialOps.Domain.WorkCenters;

namespace IndustrialOps.Application.Common.Abstractions;

public interface IWorkCenterRepository
{
    Task<IReadOnlyList<WorkCenter>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<WorkCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    void Add(WorkCenter workCenter);
}
