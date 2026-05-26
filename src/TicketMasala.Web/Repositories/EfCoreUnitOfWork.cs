using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// Unit of Work implementation using EF Core.
/// 
/// CRITICAL: This is the ONLY place where SaveChangesAsync() is called.
/// All repository write operations queue changes to the DbContext but do not commit.
/// You MUST call CommitAsync() to persist changes to the database.
/// </summary>
public class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly MasalaDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EfCoreUnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public EfCoreUnitOfWork(
        MasalaDbContext context,
        ITicketRepository ticketRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ILogger<EfCoreUnitOfWork> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public ITicketRepository Tickets => _ticketRepository;
    public IProjectRepository Projects => _projectRepository;
    public IUserRepository Users => _userRepository;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var changeCount = _context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged);

        _logger.LogDebug("Committing {ChangeCount} entity changes to database", changeCount);

        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict detected during commit");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during commit: {Message}", ex.InnerException?.Message ?? ex.Message);
            throw;
        }

        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            _logger.LogDebug("Explicit transaction committed successfully");
        }

        _logger.LogInformation("Successfully committed {Result} entity changes to database", result);
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Explicit database transaction begun");
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            _logger.LogDebug("Explicit database transaction rolled back");
        }

        // Reset change tracker to clear any tracked entities that were part of the failed transaction.
        // This prevents subsequent operations from accidentally saving changes from the rolled-back transaction.
        _context.ChangeTracker.Clear();
        _logger.LogDebug("Change tracker cleared after rollback");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }

    public Task AddQualityReviewAsync(Domain.Entities.QualityReview review, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        _context.QualityReviews.Add(review);
        _logger.LogDebug("Quality review queued for add (pending commit)");
        return Task.CompletedTask;
    }

    public Task AddTimeLogAsync(Domain.Entities.TimeLog timeLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeLog);
        _context.TimeLogs.Add(timeLog);
        _logger.LogDebug("Time log queued for add (pending commit)");
        return Task.CompletedTask;
    }

    public Task AddCommentAsync(Domain.Entities.TicketComment comment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _context.TicketComments.Add(comment);
        _logger.LogDebug("Comment queued for add to ticket {TicketId} (pending commit)", comment.TicketId);
        return Task.CompletedTask;
    }

    public Task AddOutboxMessageAsync(Domain.Entities.OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _context.OutboxMessages.Add(message);
        _logger.LogDebug(
            "Outbox message queued for add: EventType={EventType}, RoutingKey={RoutingKey} (pending commit)",
            message.EventType,
            message.RoutingKey);
        return Task.CompletedTask;
    }
}
