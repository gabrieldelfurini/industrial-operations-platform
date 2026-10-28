using FluentValidation;
using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Application.Common.Exceptions;
using IndustrialOps.Domain.WorkCenters;

namespace IndustrialOps.Application.WorkCenters;

public interface IWorkCenterService
{
    Task<IReadOnlyList<WorkCenterResponse>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<WorkCenterResponse> GetAsync(Guid id, CancellationToken ct);
    Task<WorkCenterResponse> CreateAsync(CreateWorkCenterRequest request, CancellationToken ct);
    Task<WorkCenterResponse> UpdateAsync(Guid id, UpdateWorkCenterRequest request, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
}

public sealed class WorkCenterService(
    IWorkCenterRepository workCenters,
    IUnitOfWork unitOfWork,
    IValidator<CreateWorkCenterRequest> createValidator,
    IValidator<UpdateWorkCenterRequest> updateValidator,
    TimeProvider clock) : IWorkCenterService
{
    public async Task<IReadOnlyList<WorkCenterResponse>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var items = await workCenters.ListAsync(includeInactive, ct);
        return items.Select(WorkCenterResponse.From).ToList();
    }

    public async Task<WorkCenterResponse> GetAsync(Guid id, CancellationToken ct) =>
        WorkCenterResponse.From(await LoadAsync(id, ct));

    public async Task<WorkCenterResponse> CreateAsync(CreateWorkCenterRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await workCenters.CodeExistsAsync(code, ct))
            throw new ConflictException($"A work center with code '{code}' already exists.");

        var workCenter = new WorkCenter(code, request.Name, request.Description, clock.GetUtcNow().UtcDateTime);
        workCenters.Add(workCenter);
        await unitOfWork.SaveChangesAsync(ct);
        return WorkCenterResponse.From(workCenter);
    }

    public async Task<WorkCenterResponse> UpdateAsync(Guid id, UpdateWorkCenterRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var workCenter = await LoadAsync(id, ct);
        workCenter.Update(request.Name, request.Description, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return WorkCenterResponse.From(workCenter);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var workCenter = await LoadAsync(id, ct);
        workCenter.Deactivate(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<WorkCenter> LoadAsync(Guid id, CancellationToken ct) =>
        await workCenters.GetByIdAsync(id, ct) ?? throw new NotFoundException("Work center", id);
}
