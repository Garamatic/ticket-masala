using TicketMasala.Domain.Repositories;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// Unit of Work pattern - provides transactional consistency across repositories.
/// 
/// IMPORTANT: All write operations (Add, Update, Delete) on repositories only queue
/// changes to the DbContext. You MUST call CommitAsync() to persist changes to the database.
/// This enables coordinated transactions across multiple operations.
/// 
/// Example usage:
///   await _unitOfWork.Tickets.AddAsync(ticket);
///   await _unitOfWork.AddTimeLogAsync(timeLog);
///   await _unitOfWork.CommitAsync(); // Single transaction commits both
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Ticket repository for all ticket operations.
    /// Changes are queued until CommitAsync() is called.
    /// </summary>
    ITicketRepository Tickets { get; }

    /// <summary>
    /// Project repository for all project operations.
    /// Changes are queued until CommitAsync() is called.
    /// </summary>
    IProjectRepository Projects { get; }

    /// <summary>
    /// User repository for all user operations.
    /// Changes are queued until CommitAsync() is called.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Commit all pending changes as a single transaction.
    /// This is the ONLY method that actually persists changes to the database.
    /// </summary>
    /// <returns>Number of entities affected</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a new explicit database transaction.
    /// Use this when you need to rollback on business rule violations.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current explicit transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a quality review within the current unit of work.
    /// Committed with other changes when CommitAsync() is called.
    /// </summary>
    Task AddQualityReviewAsync(Domain.Entities.QualityReview review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a time log entry within the current unit of work.
    /// Committed with other changes when CommitAsync() is called.
    /// </summary>
    Task AddTimeLogAsync(Domain.Entities.TimeLog timeLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a ticket comment within the current unit of work.
    /// Committed with other changes when CommitAsync() is called.
    /// </summary>
    Task AddCommentAsync(Domain.Entities.TicketComment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an outbox message within the current unit of work.
    /// The OutboxPublisher background service will drain this to RabbitMQ.
    /// Committed with other changes when CommitAsync() is called.
    /// </summary>
    Task AddOutboxMessageAsync(Domain.Entities.OutboxMessage message, CancellationToken cancellationToken = default);
}
