namespace TicketMasala.Web.Engine.Ingestion;

public interface ITicketGenerator
{
    Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default);
}
