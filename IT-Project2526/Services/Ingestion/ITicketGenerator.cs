namespace IT_Project2526.Services.Ingestion;;

public interface ITicketGenerator
{
    Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default);
}
