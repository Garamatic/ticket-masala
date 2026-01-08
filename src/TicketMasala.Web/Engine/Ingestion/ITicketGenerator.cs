namespace TicketMasala.Web.Engine.Ingestion;

public interface ITicketGenerator
{
    Task GenerateRandomTicketAsync(CancellationToken cancellationToken = default);
    Task GenerateGoldenPathDataAsync(CancellationToken cancellationToken = default);
}
