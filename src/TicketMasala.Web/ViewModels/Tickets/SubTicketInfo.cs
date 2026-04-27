using TicketMasala.Domain.Common;

namespace TicketMasala.Web.ViewModels.Tickets;

public class SubTicketInfo
{
    public Guid Guid { get; set; }
    public string Description { get; set; } = string.Empty;
    public Status TicketStatus { get; set; }
    public string? ResponsibleName { get; set; }
}
