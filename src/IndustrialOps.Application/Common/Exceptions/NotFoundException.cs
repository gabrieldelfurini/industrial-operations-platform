namespace IndustrialOps.Application.Common.Exceptions;

/// <summary>The requested resource does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} '{key}' was not found.");
