using IndustrialOps.Domain.Common;

namespace IndustrialOps.Domain.WorkCenters;

/// <summary>A place where production happens: a line, cell or machine (e.g. "LINE-01").</summary>
public sealed class WorkCenter : Entity
{
    private WorkCenter()
    {
    }

    public WorkCenter(string code, string name, string? description, DateTime utcNow)
        : base(utcNow)
    {
        Code = Guard.NotEmpty(code, nameof(Code)).ToUpperInvariant();
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description?.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? description, DateTime utcNow)
    {
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description?.Trim();
        Touch(utcNow);
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        Touch(utcNow);
    }
}
