namespace IndustrialOps.Application.Common.Exceptions;

/// <summary>
/// The request conflicts with the current state of the data, e.g. a duplicated code
/// or a concurrent update. Mapped to HTTP 409.
/// </summary>
public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
