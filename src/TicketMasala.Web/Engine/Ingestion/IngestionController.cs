using TicketMasala.Web.ViewModels.Ingestion;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;

namespace TicketMasala.Web.Engine.Ingestion;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly Channel<IngestionWorkItem> _channel;

    public IngestionController(Channel<IngestionWorkItem> channel)
    {
        _channel = channel;
    }

    [HttpPost]
    public IActionResult Post([FromBody] IngestionWorkItem workItem)
    {
        if (!_channel.Writer.TryWrite(workItem))
        {
            // Requirement from 09-scalability.md: "For Ticket Masala, we block (or return 503) to protect the worker."
            return StatusCode(503, "Service Unavailable - Ingestion Queue Full");
        }

        return Accepted();
    }
}