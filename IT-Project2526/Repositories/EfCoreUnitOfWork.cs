using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IT_Project2526.Repositories;

/// <summary>
/// Unit of Work implementation using EF Core.
/// Coordinates repository operations within a single transaction.
/// </summary>
public class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ITProjectDB _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public EfCoreUnitOfWork(
        ITProjectDB context,
        ITicketRepository ticketRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public ITicketRepository Tickets => _ticketRepository;
    public IProjectRepository Projects => _projectRepository;
    public IUserRepository Users => _userRepository;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);

        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
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
}
