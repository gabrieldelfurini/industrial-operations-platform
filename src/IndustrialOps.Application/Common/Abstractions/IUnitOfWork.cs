namespace IndustrialOps.Application.Common.Abstractions;

/// <summary>
/// Commits all changes made through the repositories in a single database transaction.
/// Repositories only stage changes; the service decides when the use case is done.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
