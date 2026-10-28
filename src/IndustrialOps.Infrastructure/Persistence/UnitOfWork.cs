using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IndustrialOps.Infrastructure.Persistence;

/// <summary>
/// Commits the DbContext and translates database-specific errors into application
/// exceptions, so the upper layers never need to know about EF Core or PostgreSQL.
/// </summary>
internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(
                "The resource was modified by another request. Reload it and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            // Safety net for race conditions: two requests passing the "code exists?" check at the same time.
            throw new ConflictException($"A record with the same unique value already exists ({pg.ConstraintName}).", ex);
        }
    }
}
