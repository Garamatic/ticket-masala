namespace TicketMasala.Web.ViewModels.Ingestion;

public class IngestionWorkItem
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = "{}";
    public string? Status { get; set; }
}
