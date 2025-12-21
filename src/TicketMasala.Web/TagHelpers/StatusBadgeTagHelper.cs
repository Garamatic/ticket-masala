using Microsoft.AspNetCore.Razor.TagHelpers;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.TagHelpers;

public class StatusBadgeTagHelper : TagHelper
{
    public Status TicketStatus { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";

        var cssClass = TicketStatus switch
        {
            Status.Pending => "status-pending",
            Status.Rejected => "status-rejected",
            Status.Assigned => "status-assigned",
            Status.InProgress => "status-inProgress",
            Status.Completed => "status-completed",
            Status.Failed => "status-failed",
            _ => "",
        };

        output.Attributes.SetAttribute("class", $"status-badge {cssClass}");

        output.Content.SetContent(TicketStatus.ToString());
    }
}
