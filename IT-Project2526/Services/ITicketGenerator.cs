namespace IT_Project2526.Services;

public interface ITicketGenerator
{
    Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default);
}
