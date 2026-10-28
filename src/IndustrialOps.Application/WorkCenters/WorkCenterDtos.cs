using IndustrialOps.Domain.WorkCenters;

namespace IndustrialOps.Application.WorkCenters;

public sealed record CreateWorkCenterRequest(string Code, string Name, string? Description);

public sealed record UpdateWorkCenterRequest(string Name, string? Description);

public sealed record WorkCenterResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static WorkCenterResponse From(WorkCenter w) =>
        new(w.Id, w.Code, w.Name, w.Description, w.IsActive, w.CreatedAt, w.UpdatedAt);
}
