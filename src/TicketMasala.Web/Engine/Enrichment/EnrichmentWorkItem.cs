namespace TicketMasala.Web.Engine.Enrichment;

public class EnrichmentWorkItem
{
    public Guid TicketId { get; set; }
    public string EnrichmentType { get; set; } = "All"; // "OCR", "Sentiment", "All"
}
