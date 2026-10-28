namespace IndustrialOps.Domain.Common;

/// <summary>
/// Thrown when a business rule (invariant) of the domain is violated,
/// e.g. starting a production order that was never released.
/// </summary>
public class DomainException(string message) : Exception(message);
