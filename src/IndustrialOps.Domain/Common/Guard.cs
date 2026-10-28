namespace IndustrialOps.Domain.Common;

/// <summary>
/// Small guard clauses that protect entity invariants. Input validation for the API
/// lives in the Application layer (FluentValidation); these are the last line of defense.
/// </summary>
internal static class Guard
{
    public static string NotEmpty(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");
        return value.Trim();
    }

    public static decimal Positive(decimal value, string name)
    {
        if (value <= 0)
            throw new DomainException($"{name} must be greater than zero.");
        return value;
    }

    public static decimal NotNegative(decimal value, string name)
    {
        if (value < 0)
            throw new DomainException($"{name} cannot be negative.");
        return value;
    }
}
