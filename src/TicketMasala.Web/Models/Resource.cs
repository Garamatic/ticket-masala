namespace TicketMasala.Web.Models;

public class Resource : BaseModel
{
    //Could be path or url
    public required string Location { get; set; }
    //TODO implement resource permissions
}
