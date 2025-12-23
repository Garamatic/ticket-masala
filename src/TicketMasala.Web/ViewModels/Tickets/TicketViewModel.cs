using TicketMasala.Web;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using System;

namespace TicketMasala.Web.ViewModels.Tickets;

public class TicketViewModel
{
    public Guid Guid { get; set; }
    public Status TicketStatus { get; set; }
    public string Description { get; set; } = string.Empty;

    // Add string Status property for backward compatibility with views
    public string Status => TicketStatus.ToString();

    public DateTime CreationDate { get; set; }
    public DateTime? CompletionTarget { get; set; }
    public string ResponsibleName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<string> Comments { get; set; } = new List<string>();

    public Guid? ParentTicketGuid { get; set; }
    public Guid? ProjectGuid { get; set; }
    public List<SubTicketInfo> SubTickets { get; set; } = new List<SubTicketInfo> { };
    public string? GerdaTags { get; set; }
    public TicketMasala.Domain.Services.AiExplanation? Explanation { get; set; }
}

public class SubTicketInfo
{
    public Guid Guid { get; set; }
    public Status TicketStatus { get; set; }
    public string Description { get; set; } = string.Empty;
}
