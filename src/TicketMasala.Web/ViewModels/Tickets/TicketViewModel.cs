using TicketMasala.Web;
using TicketMasala.Web.Models;
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
    public string ResponsibleName { get; set; }
    public string CustomerName { get; set; }
    public List<string> Comments { get; set; } = new List<string>();

    public Guid? ParentTicketGuid { get; set; }
    public Guid? ProjectGuid { get; set; }
    public List<SubTicketInfo> SubTickets { get; set; } = new List<SubTicketInfo> { };


}

public class SubTicketInfo
{
    public Guid Guid { get; set; }
    public Status TicketStatus { get; set; }
    public string Description { get; set; }
}
