namespace IndustrialOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Simulated reference data inserted by a migration (HasData). Values must be fixed
/// (no Guid.NewGuid / DateTime.UtcNow), otherwise EF would detect a "change" on every
/// new migration and try to update the rows again.
/// </summary>
internal static class SeedData
{
    private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly object[] Products =
    [
        Product("0199a1b2-0000-7000-8000-000000000001", "PRD-001", "Steel Bracket", "UN"),
        Product("0199a1b2-0000-7000-8000-000000000002", "PRD-002", "Aluminum Housing", "UN"),
        Product("0199a1b2-0000-7000-8000-000000000003", "PRD-003", "Hydraulic Fluid", "L"),
    ];

    public static readonly object[] WorkCenters =
    [
        WorkCenter("0199a1b2-0000-7000-8000-000000000101", "LINE-01", "Assembly Line 1", "Final assembly of brackets and housings"),
        WorkCenter("0199a1b2-0000-7000-8000-000000000102", "CNC-01", "CNC Machining Cell", "5-axis CNC machining"),
        WorkCenter("0199a1b2-0000-7000-8000-000000000103", "PAINT-01", "Paint Booth", null),
    ];

    // Anonymous objects are used because the entities have private setters/constructors.
    private static object Product(string id, string code, string name, string unit) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        UnitOfMeasure = unit,
        IsActive = true,
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
    };

    private static object WorkCenter(string id, string code, string name, string? description) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        Description = description,
        IsActive = true,
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt,
    };
}
