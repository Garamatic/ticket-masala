namespace TicketMasala.Web.Repositories;

/// <summary>
/// Unit of Work pattern - provides transactional consistency across repositories.
/// Implements the pattern from the architectural review to enable coordinated
/// database operations with proper rollback on failures.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ITicketRepository Tickets { get; }
    IProjectRepository Projects { get; }
    IUserRepository Users { get; }

    /// <summary>
    /// Commit all pending changes as a single transaction.
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a new transaction scope.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

}
